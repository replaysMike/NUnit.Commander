using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using NUnit.Commander.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NUnit.Commander
{
    public class Commander : ICommander, IDisposable
    {
        private const int LockWaitMilliseconds = 5 * 1000;
        // how long should report generation be delayed before generating, give any other clients some time to connect
        private const int ReportGenerationDelayMilliseconds = 1000;
        // how often to should draw to the screen
        internal const int DefaultDrawIntervalMilliseconds = 66;
        // how often to run utility functions
        internal const int DefaultUtilityIntervalMilliseconds = 66;
        // how often to should draw to the screen when stdout is redirected
        internal const int DefaultRedirectedDrawIntervalMilliseconds = 5000;
        // how long to should keep tests displayed after they have finished running
        internal const int DefaultActiveTestLifetimeMilliseconds = 2000;
        // how long to should keep tests displayed after they have finished running when stdout is redirected
        internal const int DefaultRedirectedActiveTestLifetimeMilliseconds = DefaultActiveTestLifetimeMilliseconds - 500;
        // how much of the test case argument to display
        internal const int MaxTestCaseArgumentLength = 20;
        // position to begin drawing at
        internal const int BeginY = 1;
        private readonly string[] DotNetRuntimes = new[] { "dotnet", "testhost.x86", "testhost" };
        private readonly string[] NUnitRuntimes = new[] { "nunit-console" };

        internal readonly IExtendedConsole _console;
        internal readonly int _activeTestLifetimeMilliseconds = DefaultActiveTestLifetimeMilliseconds;
        internal readonly int _drawIntervalMilliseconds = DefaultDrawIntervalMilliseconds;
        private readonly ICollection<Guid> _testRunIds = new List<Guid>();
        internal readonly ICollection<string> _frameworks = new List<string>();
        internal readonly ICollection<string> _frameworkVersions = new List<string>();
        internal readonly List<EventEntry> _eventLog;
        internal readonly ApplicationConfiguration _configuration;
        internal readonly PerformanceLog _performanceLog = new PerformanceLog();
        internal readonly IpcServer _ipcServer;
        internal SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        internal SemaphoreSlim _performanceLock = new SemaphoreSlim(1, 1);
        internal GrpcTestEventHost _grpcServer;
        internal CancellationTokenSource _hostCancellationToken;
        private ManualResetEvent _closeEvent;
        internal List<EventEntry> _activeTests;
        internal List<EventEntry> _activeTestFixtures;
        internal List<EventEntry> _activeAssemblies;
        internal List<EventEntry> _activeTestSuites;
        private Thread _updateThread;
        private Thread _utilityThread;
        internal bool _allowDrawActiveTests = false;
        internal int _lastNumberOfTestsRunning;
        internal int _lastNumberOfTestsDrawn;
        internal int _lastNumberOfLinesDrawn;
        internal DateTime _lastDrawTime;
        internal int _lastDrawChecksum;
        internal string _currentFramework;
        internal string _currentFrameworkVersion;
        internal int _totalTestsQueued;
        private int _performanceIteration;
        private ViewManager _viewManager;
        private bool _isDisposed;
        private CancellationTokenSource _finalReportGenerationCancelTokenSource = new CancellationTokenSource();

        /// <summary>
        /// List of tests that are currently running
        /// </summary>
        public IReadOnlyList<EventEntry> ActiveTests => new List<EventEntry>(_activeTests).AsReadOnly();

        /// <summary>
        /// List of all events
        /// </summary>
        public IReadOnlyList<EventEntry> EventLog => new List<EventEntry>(_eventLog).AsReadOnly();

        /// <summary>
        /// The report type to generate
        /// </summary>
        public GenerateReportType GenerateReportType { get; set; }

        /// <summary>
        /// Get the final run reports
        /// </summary>
        public ICollection<DataEvent> RunReports { get; private set; }

        /// <summary>
        /// Get the current run number
        /// </summary>
        public int RunNumber { get; }

        /// <summary>
        /// Get the run context
        /// </summary>
        public RunContext RunContext { get; }

        /// <summary>
        /// Get the Commander Run Id
        /// </summary>
        public Guid CommanderRunId { get; } = Guid.NewGuid();

        /// <summary>
        /// Get the start time
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Get the end time
        /// </summary>
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// Get the final report context
        /// </summary>
        public ReportContext ReportContext { get; private set; }

        /// <summary>
        /// True if commander is running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// True if commander is connected to the NUnit.Extension.TestMonitor
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// True if commander is disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Get the overall status of the run
        /// </summary>
        public TestStatus TestStatus { get; private set; } = TestStatus.Running;

        /// <summary>
        /// Set/Get the current color scheme
        /// </summary>
        public ColorScheme ColorScheme { get; }


        public Commander(ApplicationConfiguration configuration, ColorScheme colorScheme)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ColorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));
            IsRunning = true;
            GenerateReportType = configuration.GenerateReportType;
            _closeEvent = new ManualResetEvent(false);
            _eventLog = new List<EventEntry>();
            _activeTests = new List<EventEntry>();
            _activeTestFixtures = new List<EventEntry>();
            _activeAssemblies = new List<EventEntry>();
            _activeTestSuites = new List<EventEntry>();
            _viewManager = new ViewManager(new ViewContext(this), ViewPages.ActiveTests);
            RunReports = new List<DataEvent>();
            _ipcServer = new IpcServer(configuration);
            _ipcServer.OnMessageReceived += IpcServer_OnMessageReceived;
            _grpcServer = new GrpcTestEventHost(configuration);
            _grpcServer.TestEventReceived += GrpcHost_TestEventReceived;

            // start the display thread
            _updateThread = new Thread(new ThreadStart(DisplayThread));
            _updateThread.IsBackground = false;
            _updateThread.Priority = ThreadPriority.AboveNormal;
            _updateThread.Name = nameof(DisplayThread);
            _updateThread.Start();

            // start the utility thread
            _utilityThread = new Thread(new ThreadStart(UtilityThread));
            _utilityThread.IsBackground = true;
            _utilityThread.Name = nameof(UtilityThread);
            _utilityThread.Start();

            _hostCancellationToken = new CancellationTokenSource();
            _grpcServer.RunAsync(_hostCancellationToken.Token);
            _ipcServer.Start();
        }

        public Commander(ApplicationConfiguration configuration, ColorScheme colorScheme, IExtendedConsole console, int runNumber, RunContext runContext) : this(configuration, colorScheme)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (colorScheme == null)
                throw new ArgumentNullException(nameof(colorScheme));
            if (console == null)
                throw new ArgumentNullException(nameof(console));
            if (runContext == null)
                throw new ArgumentNullException(nameof(runContext));
            _console = new ConsoleWrapper(console, configuration);
            RunNumber = runNumber;
            RunContext = runContext;
            _activeTestLifetimeMilliseconds = configuration.ActiveTestLifetimeMilliseconds > 0 ? configuration.ActiveTestLifetimeMilliseconds : DefaultActiveTestLifetimeMilliseconds;
            if (_console.IsOutputRedirected)
            {
                _activeTestLifetimeMilliseconds = configuration.RedirectedActiveTestLifetimeMilliseconds > 0 ? configuration.RedirectedActiveTestLifetimeMilliseconds : DefaultRedirectedActiveTestLifetimeMilliseconds;
                _drawIntervalMilliseconds = configuration.RedirectedDrawIntervalMilliseconds > 0 ? configuration.RedirectedDrawIntervalMilliseconds : DefaultRedirectedDrawIntervalMilliseconds;
            }
        }

        public void PauseDisplay()
        {
            _viewManager?.PauseDisplay();
        }

        public void UnpauseDisplay()
        {
            _viewManager?.UnpauseDisplay();
        }

        public void TogglePauseDisplay()
        {
            _viewManager?.TogglePauseDisplay();
        }

        public void PreviousView()
        {
            _viewManager?.PreviousView();
        }

        public void NextView()
        {
            _viewManager?.NextView();
        }

        public void SetView(ViewPages view)
        {
            _viewManager?.SetView(view);
        }

        private void GrpcHost_TestEventReceived(object sender, MessageEventArgs e)
        {
            ReceiveMessage(sender, e);
        }

        private void IpcServer_OnMessageReceived(object sender, MessageEventArgs e)
        {
            ReceiveMessage(sender, e);
        }

        private void ReceiveMessage(object sender, MessageEventArgs e)
        {
            // a new message has been received from the IpcServer
            _lock?.Wait();
            // cancel any pending shutdown events
            _finalReportGenerationCancelTokenSource?.Cancel();
            if (StartTime == DateTime.MinValue)
                StartTime = DateTime.Now;
            if (RunContext?.StartTime == DateTime.MinValue)
                RunContext.StartTime = DateTime.Now;
            try
            {
                //System.Diagnostics.Debug.WriteLine($"Received Message {e.EventEntry.Event.Event} - {e.EventEntry.Event.TestName}, {e.EventEntry.Event.TestStatus} {e.EventEntry.Event.TestSuite}");
                // inject data
                e.EventEntry.Event.RunNumber = RunNumber;

                _eventLog.Add(e.EventEntry);

                if (!IsTestRunIdReceived(e.EventEntry.Event.TestRunId))
                    AddTestRunId(e.EventEntry.Event.TestRunId);
                if (!IsFrameworkReceived(e.EventEntry.Event.Runtime))
                    AddFramework(e.EventEntry.Event.Runtime);
                if (!IsFrameworkVersionReceived(e.EventEntry.Event.RuntimeVersion))
                    AddFrameworkVersion(e.EventEntry.Event.RuntimeVersion);
                ProcessActiveTests(e.EventEntry);

                if (e.EventEntry.Event.TestStatus == TestStatus.Fail && e.EventEntry.Event.Event == EventNames.EndTest)
                {
                    // if we are logging to a file, and a test has failed write it immediately to the output
                    if (_console.IsOutputRedirected)
                    {
                        _console.WriteLine($"{Environment.NewLine}Failed test: {e.EventEntry.Event.FullName} {UTF8Constants.LeftBracket}{DateTime.Now}{UTF8Constants.RightBracket}");
                        if (!string.IsNullOrEmpty(e.EventEntry.Event.ErrorMessage))
                            _console.WriteLine($"  Test Error: {e.EventEntry.Event.ErrorMessage}");
                        if (!string.IsNullOrEmpty(e.EventEntry.Event.StackTrace))
                            _console.WriteLine($"  Stack Trace: {e.EventEntry.Event.StackTrace}");
                        if (!string.IsNullOrEmpty(e.EventEntry.Event.TestOutput))
                            _console.WriteLine($"  Test Output: {e.EventEntry.Event.TestOutput}");
                    }

                    if (_configuration.ExitOnFirstTestFailure)
                    {
                        // close commander immediately on test failure
                        RunContext.EndTime = DateTime.Now;
                        Close();
                    }
                }
            }
            finally
            {
                _lock?.Release();
            }

            if (e.EventEntry.Event.Event == EventNames.Report)
            {
                //System.Diagnostics.Debug.WriteLine($"END report received for {e.EventEntry.Event.TestName}!");
                _allowDrawActiveTests = true;
                // System.Diagnostics.Debug.WriteLine($"Active Test Suites: ({string.Join(",", _activeTestSuites.Where(x => !x.IsQueuedForRemoval))}), Active Assemblies: ({string.Join(",", _activeAssemblies.Where(x => !x.IsQueuedForRemoval))})");
                if (_activeAssemblies.Count(x => !x.IsQueuedForRemoval) == 0)
                {
                    //System.Diagnostics.Debug.WriteLine($"No active tests running, generating report!");
                    RunContext.EndTime = DateTime.Now;
                    ScheduleFinalizeWithDelay();
                }
            }
        }

        /// <summary>
        /// Schedules program completion and final report generation that can be cancelled
        /// </summary>
        private void ScheduleFinalizeWithDelay()
        {
            _finalReportGenerationCancelTokenSource = new CancellationTokenSource();
            try
            {
                var task = new Task(async () =>
                {
                    // wait for a bit to give more clients a chance to connect
                    try
                    {
                        FinalizeTestRun();
                        await Task.Delay(TimeSpan.FromMilliseconds(ReportGenerationDelayMilliseconds), _finalReportGenerationCancelTokenSource.Token);
                        // proceed with report generation
                        Close();
                    }
                    catch (TaskCanceledException)
                    {
                        // wait for another finalize event
                    }
                }, _finalReportGenerationCancelTokenSource.Token);
                task.Start();
            }
            catch (TaskCanceledException)
            {
                // wait for another finalize event
            }
        }

        public void WaitForClose()
        {
            _closeEvent?.WaitOne();
        }

        public void WaitForClose(int millisecondsTimeout)
        {
            _closeEvent?.WaitOne(millisecondsTimeout);
        }

        public void Close()
        {
            IsRunning = false;
            _closeEvent?.Set();
        }

        private void DisplayThread()
        {
            var iteration = 0L;
            while (_closeEvent?.WaitOne(_drawIntervalMilliseconds) == false)
            {
                RemoveExpiredActiveTests();
                _viewManager.Draw(iteration);
                LogPerformance();
                Interlocked.Increment(ref iteration);
            }
        }

        private void UtilityThread()
        {
            while (_closeEvent?.WaitOne(DefaultUtilityIntervalMilliseconds) == false)
            {
                LogPerformance();
            }
        }

        private void LogPerformance()
        {
            _performanceLock?.Wait();
            try
            {
                _performanceIteration++;
                if (_eventLog.Count > 0 && _performanceIteration > 30 && _performanceIteration % 10 == 0)
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    if (RunContext?.PerformanceCounters?.CpuCounter != null)
                        _performanceLog.AddEntry(PerformanceLog.PerformanceType.CpuUsed, RunContext.PerformanceCounters.CpuCounter.NextValue());
                    if (RunContext?.PerformanceCounters?.DiskCounter != null)
                        _performanceLog.AddEntry(PerformanceLog.PerformanceType.DiskTime, RunContext.PerformanceCounters.DiskCounter.NextValue());
#pragma warning restore CA1416 // Validate platform compatibility
                    var activeTestCount = 0;
                    var activeTestFixtureCount = 0;
                    var activeAssembliesCount = 0;
                    _lock.Wait();
                    try
                    {
                        activeTestCount = _activeTests.Count(x => !x.IsQueuedForRemoval);
                        activeTestFixtureCount = _activeTestFixtures.Count(x => !x.IsQueuedForRemoval);
                        activeAssembliesCount = _activeAssemblies.Count(x => !x.IsQueuedForRemoval);
                        if (activeTestCount > 0 && activeTestFixtureCount == 0)
                        {
                            // fix for older versions of nUnit that don't send the start test fixture event
                            // it will still be incorrect, as it will treat parents of tests with multiple cases as a testfixture
                            var parentIds = _activeTests
                                .Where(x => !x.IsQueuedForRemoval && !string.IsNullOrEmpty(x.Event.ParentId))
                                .Select(x => x.Event.ParentId)
                                .Distinct();
                            activeTestFixtureCount = _activeTestSuites.Count(x => !x.IsQueuedForRemoval && parentIds.Contains(x.Event.Id));
                        }
                    }
                    finally
                    {
                        _lock.Release();
                    }
                    _performanceLog.AddEntry(PerformanceLog.PerformanceType.TestConcurrency, activeTestCount);
                    _performanceLog.AddEntry(PerformanceLog.PerformanceType.TestFixtureConcurrency, activeTestFixtureCount);
                    _performanceLog.AddEntry(PerformanceLog.PerformanceType.AssemblyConcurrency, activeAssembliesCount);

                    // we don't use a performance counter for memory, this is more accurate
                    var availableMemoryBytes = PerformanceInfo.GetPhysicalAvailableMemoryInMiB() * 1024;
                    var totalMemoryBytes = PerformanceInfo.GetTotalMemoryInMiB() * 1024;
                    _performanceLog.AddEntry(PerformanceLog.PerformanceType.MemoryUsed, totalMemoryBytes - availableMemoryBytes);
                }
            }
            finally
            {
                _performanceLock?.Release();
            }
        }

        private bool IsTestRunIdReceived(Guid? testRunId)
        {
            return _testRunIds.Contains(testRunId ?? Guid.Empty);
        }

        private void AddTestRunId(Guid? testRunId)
        {
            if (testRunId.HasValue)
                _testRunIds.Add(testRunId.Value);
        }

        private bool IsFrameworkReceived(string framework)
        {
            return _frameworks.Contains(framework);
        }

        private void AddFramework(string framework)
        {
            if (!string.IsNullOrEmpty(framework))
            {
                _frameworks.Add(framework);
                _currentFramework = framework;
            }
        }

        private bool IsFrameworkVersionReceived(string frameworkVersion)
        {
            return _frameworkVersions.Contains(frameworkVersion);
        }

        private void AddFrameworkVersion(string frameworkVersion)
        {
            if (!string.IsNullOrEmpty(frameworkVersion))
            {
                _frameworkVersions.Add(frameworkVersion);
                _currentFrameworkVersion = frameworkVersion;
            }
        }

        public ReportContext GenerateReportContext(bool isExclusive = true)
        {
            _performanceLock.Wait();
            if (isExclusive)
                _lock.Wait();
            try
            {
                return new ReportContext
                {
                    CommanderRunId = CommanderRunId,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    FrameworkRuntimes = new List<string>(_frameworkVersions),
                    Frameworks = new List<string>(_frameworks),
                    TestRunIds = new List<Guid>(_testRunIds),
                    EventEntries = new List<EventEntry>(_eventLog),
                    PerformanceLog = _performanceLog,
                    Performance = new PerformanceOverview
                    {
                        PeakCpuUsed = _performanceLog.GetPeak(PerformanceLog.PerformanceType.CpuUsed),
                        MedianCpuUsed = _performanceLog.GetMedian(PerformanceLog.PerformanceType.CpuUsed),
                        PeakMemoryUsed = _performanceLog.GetPeak(PerformanceLog.PerformanceType.MemoryUsed),
                        MedianMemoryUsed = _performanceLog.GetMedian(PerformanceLog.PerformanceType.MemoryUsed),
                        PeakDiskTime = _performanceLog.GetPeak(PerformanceLog.PerformanceType.DiskTime),
                        MedianDiskTime = _performanceLog.GetMedian(PerformanceLog.PerformanceType.DiskTime),
                        PeakTestConcurrency = _performanceLog.GetPeak(PerformanceLog.PerformanceType.TestConcurrency),
                        MedianTestConcurrency = _performanceLog.GetMedian(PerformanceLog.PerformanceType.TestConcurrency),
                        PeakTestFixtureConcurrency = _performanceLog.GetPeak(PerformanceLog.PerformanceType.TestFixtureConcurrency),
                        MedianTestFixtureConcurrency = _performanceLog.GetMedian(PerformanceLog.PerformanceType.TestFixtureConcurrency),
                        PeakAssemblyConcurrency = _performanceLog.GetPeak(PerformanceLog.PerformanceType.AssemblyConcurrency),
                        MedianAssemblyConcurrency = _performanceLog.GetMedian(PerformanceLog.PerformanceType.AssemblyConcurrency),
                    }
                };
            }
            finally
            {
                _performanceLock.Release();
                if (isExclusive)
                    _lock.Release();
            }
        }

        private void FinalizeTestRun()
        {
            IsRunning = false;
            EndTime = DateTime.Now;

            _console?.WriteLine($"Finalizing test run...");
            var anyFailures = RunReports.SelectMany(x => x.Report.TestReports).Any(x => x.TestStatus == TestStatus.Fail);
            if (anyFailures)
                TestStatus = TestStatus.Fail;
            else
                TestStatus = TestStatus.Pass;
            if (_console?.IsOutputRedirected != true)
            {
                _console?.ClearAtRange(0, BeginY, 0, BeginY + 1 + _lastNumberOfLinesDrawn);
                _console?.SetCursorPosition(0, BeginY);
            }
        }

        private void RemoveExpiredActiveTests()
        {
            _lock.Wait();
            try
            {
                var testsRemoved = _activeTests.RemoveAll(x => x.RemovalTime != DateTime.MinValue && x.RemovalTime < DateTime.Now);
                if (testsRemoved > 0)
                    System.Diagnostics.Debug.WriteLine($"REMOVED {testsRemoved} tests");
                var testFixturesRemoved = _activeTestFixtures.RemoveAll(x => x.RemovalTime != DateTime.MinValue && x.RemovalTime < DateTime.Now);
                if (testFixturesRemoved > 0)
                    System.Diagnostics.Debug.WriteLine($"REMOVED {testFixturesRemoved} test fixtures");
                var testSuitesRemoved = _activeTestSuites.RemoveAll(x => x.RemovalTime != DateTime.MinValue && x.RemovalTime < DateTime.Now);
                if (testSuitesRemoved > 0)
                    System.Diagnostics.Debug.WriteLine($"REMOVED {testSuitesRemoved} test suites");
                var assembliesRemoved = _activeAssemblies.RemoveAll(x => x.RemovalTime != DateTime.MinValue && x.RemovalTime < DateTime.Now);
                if (assembliesRemoved > 0)
                    System.Diagnostics.Debug.WriteLine($"REMOVED {assembliesRemoved} assemblies");
            }
            finally
            {
                _lock.Release();
            }
        }

        private void UpdateEventEntry(EventEntry existingEvent, EventEntry newEvent)
        {
            // update an existing event with data from a new event
            if (existingEvent != null)
            {
                // update the active test information
                if (newEvent.Event.IsSkipped)
                    // remove skipped tests immediately
                    existingEvent.RemovalTime = DateTime.Now;
                else
                    existingEvent.RemovalTime = DateTime.Now.AddMilliseconds(_activeTestLifetimeMilliseconds);
                existingEvent.Event.Duration = newEvent.Event.Duration;
                existingEvent.Event.EndTime = newEvent.Event.EndTime;
                existingEvent.Event.TestResult = newEvent.Event.TestResult;
                existingEvent.Event.TestStatus = newEvent.Event.TestStatus;
                existingEvent.Event.IsSkipped = newEvent.Event.IsSkipped;
                existingEvent.Event.ErrorMessage = newEvent.Event.ErrorMessage;
                existingEvent.Event.StackTrace = newEvent.Event.StackTrace;
            }
        }

        private void ProcessActiveTests(EventEntry e)
        {
            // Debug.WriteLine($"EVENT: {e.Event.Event}");
            switch (e.Event.Event)
            {
                case EventNames.StartRun:
                    _totalTestsQueued += e.Event.TestCount;
                    break;
                case EventNames.StartAssembly:
                    _activeAssemblies.Add(new EventEntry(e));
                    break;
                case EventNames.EndAssembly:
                    var matchingActiveAssembly = _activeAssemblies.FirstOrDefault(x => x.Event.TestSuite == e.Event.TestSuite && x.Event.Event == EventNames.StartAssembly);
                    UpdateEventEntry(matchingActiveAssembly, e);
                    break;
                case EventNames.StartSuite:
                    if (!string.IsNullOrEmpty(e.Event.TestSuite) && e.Event.TestSuite.EndsWith(".dll"))
                    {
                        // fix for older versions of nunit
                        e.Event.Event = EventNames.StartAssembly;
                        _activeAssemblies.Add(new EventEntry(e));
                    }
                    else
                        _activeTestSuites.Add(new EventEntry(e));
                    break;
                case EventNames.EndSuite:
                    var matchingActiveTestSuite = _activeTestSuites.FirstOrDefault(x => x.Event.TestSuite == e.Event.TestSuite && x.Event.Event == EventNames.StartSuite);
                    UpdateEventEntry(matchingActiveTestSuite, e);
                    break;
                case EventNames.StartTestFixture:
                    _activeTestFixtures.Add(new EventEntry(e));
                    break;
                case EventNames.EndTestFixture:
                    var matchingActiveTestFixture = _activeTestFixtures.FirstOrDefault(x => x.Event.FullName == e.Event.FullName && x.Event.Event == EventNames.StartTestFixture);
                    UpdateEventEntry(matchingActiveTestFixture, e);
                    break;
                case EventNames.StartTest:
                    // clone the event object
                    _activeTests.Add(new EventEntry(e));
                    break;
                case EventNames.EndTest:
                    var matchingActiveTest = _activeTests.FirstOrDefault(x => x.Event.FullName == e.Event.FullName && x.Event.Event == EventNames.StartTest);
                    UpdateEventEntry(matchingActiveTest, e);
                    break;
                case EventNames.Report:
                    RunReports.Add(e.Event);
                    break;
                default:
                    // unknown event type
                    break;
            }
        }

        public DataEvent CreateReportFromHistory(bool requiresLock = true)
        {
            if (requiresLock)
                _lock.Wait();
            try
            {
                var completedTests = _eventLog.Where(x => x.Event.Event == EventNames.EndTest);
                var report = new DataEvent(EventNames.Report);
                report.TestCount = completedTests.Count();
                report.Passed = completedTests.Where(x => x.Event.TestStatus == TestStatus.Pass).Count();
                report.Failed = completedTests.Where(x => x.Event.TestStatus == TestStatus.Fail).Count();
                report.Skipped = completedTests.Where(x => x.Event.TestStatus == TestStatus.Skipped).Count();
                report.Warnings = completedTests.Sum(x => x.Event.Warnings);
                report.Asserts = completedTests.Sum(x => x.Event.Asserts);
                report.StartTime = StartTime;
                report.EndTime = DateTime.Now;
                report.Duration = report.EndTime.Subtract(StartTime);
                report.Runtime = _currentFramework;
                report.RuntimeVersion = _currentFrameworkVersion;
                report.TestRunId = _testRunIds.LastOrDefault();
                report.TestStatus = TestStatus.Fail;
                report.Report = new DataReport()
                {
                    TestReports = completedTests.Select(x => new TestCaseReport
                    {
                        Asserts = x.Event.Asserts,
                        Duration = x.Event.Duration,
                        EndTime = x.Event.EndTime,
                        ErrorMessage = x.Event.ErrorMessage,
                        FullName = x.Event.FullName,
                        Id = x.Event.Id,
                        IsSkipped = x.Event.IsSkipped,
                        Runtime = x.Event.Runtime,
                        RuntimeVersion = x.Event.RuntimeVersion,
                        StackTrace = x.Event.StackTrace,
                        StartTime = x.Event.StartTime,
                        TestName = x.Event.TestName,
                        TestOutput = x.Event.TestOutput,
                        TestResult = x.Event.TestResult,
                        TestStatus = x.Event.TestStatus,
                        TestSuite = x.Event.TestSuite
                    }).ToList(),
                    TotalTests = completedTests.Count()
                };
                return report;
            }
            finally
            {
                if (requiresLock)
                    _lock.Release();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            _isDisposed = true;
            IsRunning = false;
            if (isDisposing)
            {
                _lock.Wait(LockWaitMilliseconds);
                _performanceLock.Wait(LockWaitMilliseconds);
                try
                {
                    //_grpcServer?.StopAsync();
                    _hostCancellationToken.Cancel();
                    _ipcServer?.Dispose();
                    _closeEvent?.Set();
                    _closeEvent?.Dispose();
                    _closeEvent = null;
                    _updateThread = null;
                    _utilityThread = null;
                    _console?.Dispose();
                }
                finally
                {
                    _lock.Release();
                    _lock.Dispose();
                    _lock = null;
                    _performanceLock.Release();
                    _performanceLock.Dispose();
                    _performanceLock = null;
                }
            }
        }
    }
}
