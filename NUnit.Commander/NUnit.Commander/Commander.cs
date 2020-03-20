using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NUnit.Commander
{
    public class Commander : ICommander, IDisposable
    {
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
        internal readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        internal readonly ApplicationConfiguration _configuration;
        internal IpcClient _client;
        private ManualResetEvent _closeEvent;
        internal List<EventEntry> _activeTests;
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
        private PerformanceLog _performanceLog = new PerformanceLog();
        private int _performanceIteration;
        private ViewManager _viewManager;

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
        /// Get the overall status of the run
        /// </summary>
        public TestStatus TestStatus { get; private set; } = TestStatus.Running;

        /// <summary>
        /// Set/Get the current color scheme
        /// </summary>
        public ColorManager ColorScheme { get; set; }


        public Commander(ApplicationConfiguration configuration)
        {
            _configuration = configuration;
            GenerateReportType = configuration.GenerateReportType;
            _closeEvent = new ManualResetEvent(false);
            _eventLog = new List<EventEntry>();
            _activeTests = new List<EventEntry>();
            _viewManager = new ViewManager(new ViewContext(this), ViewPages.ActiveTests);
            RunReports = new List<DataEvent>();
            ColorScheme = new ColorManager(_configuration.ColorScheme);

            // start the display thread
            _updateThread = new Thread(new ThreadStart(UpdateThread));
            _updateThread.IsBackground = true;
            _updateThread.Name = "UpdateThread";
            _updateThread.Start();

            // start the utility thread

            _utilityThread = new Thread(new ThreadStart(UtilityThread));
            _utilityThread.IsBackground = true;
            _utilityThread.Name = "UtilityThread";
            _utilityThread.Start();
            StartTime = DateTime.Now;
        }

        public Commander(ApplicationConfiguration configuration, IExtendedConsole console, int runNumber, RunContext runContext) : this(configuration)
        {
            _console = new ConsoleWrapper(console, configuration);
            _client = new IpcClient(configuration, console);
            _client.OnMessageReceived += IpcClient_OnMessageReceived;
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

        private void IpcClient_OnMessageReceived(object sender, MessageEventArgs e)
        {
            // a new message has been received from the IpcServer
            _lock.Wait();
            try
            {
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
                        _console.WriteLine($"{Environment.NewLine}Failed test: {e.EventEntry.Event.FullName} [{DateTime.Now}]");
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
                        Close();
                    }
                }
            }
            finally
            {
                _lock.Release();
            }

            if (e.EventEntry.Event.Event == EventNames.Report)
            {
                _allowDrawActiveTests = false;
                // when we receive a report, we need to reconnect to the IpcServer as DotNetTest behaves differently than
                // NUnit in this manner. It will run tests in new processes, so we need to reconnect and see if more tests are running.
                if (DotNetRuntimes.Contains(e.EventEntry.Event.TestRunner))
                {
                    // disconnect from the server, and wait for a new connection to appear
                    Debug.WriteLine($"Waiting for another server connection...");
                    _console.WriteLine($"Waiting for another server connection...");
                    _client.Dispose();
                    // if the connection fails, write the report
                    Connect(false, (x) => { }, (x) =>
                    {
                        FinalizeTestRun();
                        x.Close();
                    });
                    return;
                }

                // NUnit does not require reconnections
                FinalizeTestRun();
                Close();
            }
        }

        public void Connect(bool showOutput, Action<ICommander> onSuccessConnect, Action<ICommander> onFailedConnect)
        {
            var extensionName = "NUnit.Extension.TestMonitor";
            if (showOutput)
            {
                var timeoutStr = $" (Timeout: {(_configuration.ConnectTimeoutSeconds > 0 ? $"{_configuration.ConnectTimeoutSeconds} seconds" : "none")})";
                Console.ForegroundColor = ColorScheme.GetMappedConsoleColor(ColorScheme.Default);
                _console.WriteLine($"Connecting to {extensionName}{timeoutStr}...");
            }

            _client.Connect(showOutput, (client) =>
            {
                // successful connect
                _allowDrawActiveTests = true;
                IsRunning = true;
                if (showOutput)
                {
                    Console.ForegroundColor = ColorScheme.GetMappedConsoleColor(ColorScheme.Default);
                    _console.WriteLine($"Connected to {extensionName}, Run #{RunNumber}.");
                    if (_console.IsOutputRedirected)
                        _console.WriteLine($"Tests started at {DateTime.Now}");
                    onSuccessConnect(this);
                }
            }, (client) =>
            {
                IsRunning = false;
                // failed connect
                if (showOutput)
                {
                    Console.ForegroundColor = ColorScheme.GetMappedConsoleColor(ColorScheme.Default);
                    _console.WriteLine(ColorTextBuilder.Create.AppendLine($"Failed to connect to {extensionName} extension within {_configuration.ConnectTimeoutSeconds} seconds.", ColorScheme.Error));
                    _console.WriteLine($"Please ensure your test runner is launched and the {extensionName} extension is correctly configured.");
                    _console.WriteLine(ColorTextBuilder.Create.Append("Try using --help, or see ").Append($"https://github.com/replaysMike/{extensionName}", ColorScheme.Highlight).AppendLine(" for more details."));
                }
                onFailedConnect(this);
            });
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
            EndTime = DateTime.Now;
            _closeEvent?.Set();
        }

        private void UpdateThread()
        {
            var iteration = 0L;
            while (!_closeEvent.WaitOne(_drawIntervalMilliseconds))
            {
                RemoveExpiredActiveTests();
                _viewManager.Draw(iteration);
                LogPerformance();
                Interlocked.Increment(ref iteration);
            }
        }

        private void UtilityThread()
        {
            while (!_closeEvent.WaitOne(DefaultUtilityIntervalMilliseconds))
            {
                LogPerformance();
            }
        }

        private void LogPerformance()
        {
            _performanceIteration++;
            if (_eventLog.Count > 0 && _performanceIteration > 30 && _performanceIteration % 10 == 0)
            {
                if (RunContext?.PerformanceCounters?.CpuCounter != null)
                    _performanceLog.AddEntry(PerformanceLog.PerformanceType.CpuUsed, RunContext.PerformanceCounters.CpuCounter.NextValue());
                if (RunContext?.PerformanceCounters?.DiskCounter != null)
                    _performanceLog.AddEntry(PerformanceLog.PerformanceType.DiskTime, RunContext.PerformanceCounters.DiskCounter.NextValue());
                _performanceLog.AddEntry(PerformanceLog.PerformanceType.Concurrency, _activeTests.Count(x => !x.IsQueuedForRemoval));

                // we don't use a performance counter for memory, this is more accurate
                var availableMemoryBytes = PerformanceInfo.GetPhysicalAvailableMemoryInMiB() * 1024;
                var totalMemoryBytes = PerformanceInfo.GetTotalMemoryInMiB() * 1024;
                _performanceLog.AddEntry(PerformanceLog.PerformanceType.MemoryUsed, totalMemoryBytes - availableMemoryBytes);
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

        public ReportContext GenerateReportContext()
        {
            return new ReportContext
            {
                CommanderRunId = CommanderRunId,
                StartTime = StartTime,
                EndTime = EndTime,
                FrameworkRuntimes = _frameworkVersions,
                Frameworks = _frameworks,
                TestRunIds = _testRunIds,
                EventEntries = _eventLog,
                Performance = new ReportContext.PerformanceOverview
                {
                    PeakCpuUsed = _performanceLog.GetPeak(PerformanceLog.PerformanceType.CpuUsed),
                    MedianCpuUsed = _performanceLog.GetMedian(PerformanceLog.PerformanceType.CpuUsed),
                    PeakMemoryUsed = _performanceLog.GetPeak(PerformanceLog.PerformanceType.MemoryUsed),
                    MedianMemoryUsed = _performanceLog.GetMedian(PerformanceLog.PerformanceType.MemoryUsed),
                    PeakDiskTime = _performanceLog.GetPeak(PerformanceLog.PerformanceType.DiskTime),
                    MedianDiskTime = _performanceLog.GetMedian(PerformanceLog.PerformanceType.DiskTime),
                    PeakConcurrency = _performanceLog.GetPeak(PerformanceLog.PerformanceType.Concurrency),
                    MedianConcurrency = _performanceLog.GetMedian(PerformanceLog.PerformanceType.Concurrency),
                }
            };
        }

        private void FinalizeTestRun()
        {
            IsRunning = false;
            EndTime = DateTime.Now;

            _console.WriteLine($"Finalizing test run...");
            var anyFailures = RunReports.SelectMany(x => x.Report.TestReports).Any(x => x.TestStatus == TestStatus.Fail);
            if (anyFailures)
                TestStatus = TestStatus.Fail;
            else
                TestStatus = TestStatus.Pass;
            if (!_console.IsOutputRedirected)
            {
                _console.ClearAtRange(0, BeginY, 0, BeginY + 1 + _lastNumberOfLinesDrawn);
                _console.SetCursorPosition(0, BeginY);
            }
        }

        private void RemoveExpiredActiveTests()
        {
            _lock.Wait();
            try
            {
                var testsRemoved = _activeTests.RemoveAll(x => x.RemovalTime != DateTime.MinValue && x.RemovalTime < DateTime.Now);
                if (testsRemoved > 0)
                {
                    // Debug.WriteLine($"REMOVED {testsRemoved} tests");
                }
            }
            finally
            {
                _lock.Release();
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
                case EventNames.StartTest:
                    // clone the event object
                    _activeTests.Add(new EventEntry(e));
                    break;
                case EventNames.EndTest:
                    var matchingActiveTest = _activeTests.FirstOrDefault(x => x.Event.Id == e.Event.Id && x.Event.Event == EventNames.StartTest);
                    if (matchingActiveTest != null)
                    {
                        // update the active test information
                        if (e.Event.IsSkipped)
                            // remove skipped tests immediately
                            matchingActiveTest.RemovalTime = DateTime.Now;
                        else
                            matchingActiveTest.RemovalTime = DateTime.Now.AddMilliseconds(_activeTestLifetimeMilliseconds);
                        matchingActiveTest.Event.Duration = e.Event.Duration;
                        matchingActiveTest.Event.EndTime = e.Event.EndTime;
                        matchingActiveTest.Event.TestResult = e.Event.TestResult;
                        matchingActiveTest.Event.TestStatus = e.Event.TestStatus;
                        matchingActiveTest.Event.IsSkipped = e.Event.IsSkipped;
                        matchingActiveTest.Event.ErrorMessage = e.Event.ErrorMessage;
                        matchingActiveTest.Event.StackTrace = e.Event.StackTrace;
                        // Debug.WriteLine($"Set removal time to {matchingActiveTest.RemovalTime.Subtract(DateTime.Now)} for test {matchingActiveTest.Event.TestName}");
                    }
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
            if (isDisposing)
            {
                _lock.Wait();
                try
                {
                    _client?.Dispose();
                    _closeEvent?.Set();
                    if (_updateThread?.Join(5 * 1000) == false)
                        _updateThread.Abort();
                    if (_utilityThread?.Join(5 * 1000) == false)
                        _utilityThread.Abort();
                    _closeEvent?.Dispose();
                    _closeEvent = null;
                    _client = null;
                    _updateThread = null;
                    _utilityThread = null;
                    _console?.Dispose();
                }
                finally
                {
                    _lock.Release();
                    _lock.Dispose();
                }
            }
        }
    }
}
