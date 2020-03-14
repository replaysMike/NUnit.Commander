using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
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
        private const int DefaultDrawIntervalMilliseconds = 66;
        // how often to run utility functions
        private const int DefaultUtilityIntervalMilliseconds = 66;
        // how often to should draw to the screen when stdout is redirected
        private const int DefaultRedirectedDrawIntervalMilliseconds = 5000;
        // how long to should keep tests displayed after they have finished running
        private const int DefaultActiveTestLifetimeMilliseconds = 2000;
        // how long to should keep tests displayed after they have finished running when stdout is redirected
        private const int DefaultRedirectedActiveTestLifetimeMilliseconds = DefaultActiveTestLifetimeMilliseconds - 500;
        // how much of the test case argument to display
        private const int MaxTestCaseArgumentLength = 20;
        // position to begin drawing at
        private const int BeginY = 1;
        private readonly string[] DotNetRuntimes = new[] { "dotnet", "testhost.x86", "testhost" };
        private readonly string[] NUnitRuntimes = new[] { "nunit-console" };

        private readonly IExtendedConsole _console;
        private readonly int _activeTestLifetimeMilliseconds = DefaultActiveTestLifetimeMilliseconds;
        private readonly int _drawIntervalMilliseconds = DefaultDrawIntervalMilliseconds;
        private readonly ICollection<Guid> _testRunIds = new List<Guid>();
        private readonly ICollection<string> _frameworks = new List<string>();
        private readonly ICollection<string> _frameworkVersions = new List<string>();
        private readonly IList<EventEntry> _eventLog;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ApplicationConfiguration _configuration;
        private IpcClient _client;
        private ManualResetEvent _closeEvent;
        private List<EventEntry> _activeTests;
        private Thread _updateThread;
        private Thread _utilityThread;
        private bool _allowDrawActiveTests = false;
        private int _lastNumberOfTestsRunning;
        private int _lastNumberOfTestsDrawn;
        private int _lastNumberOfLinesDrawn;
        private DateTime _lastDrawTime;
        private int _lastDrawChecksum;
        private string _currentFramework;
        private string _currentFrameworkVersion;
        private int _totalTestsQueued;
        private PerformanceLog _performanceLog = new PerformanceLog();
        private int _performanceIteration;

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
            while (!_closeEvent.WaitOne(_drawIntervalMilliseconds))
            {
                RemoveExpiredActiveTests();
                DisplayActiveTests();
                LogPerformance();
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

                // we don't use a performance counter for memory, this is more accurate
                var availableMemoryBytes = PerformanceInfo.GetPhysicalAvailableMemoryInMiB() * 1024;
                var totalMemoryBytes = PerformanceInfo.GetTotalMemoryInMiB() * 1024;
                _performanceLog.AddEntry(PerformanceLog.PerformanceType.MemoryUsed, totalMemoryBytes - availableMemoryBytes);
            }
        }

        private void DisplayActiveTests()
        {
            if (!_allowDrawActiveTests)
                return;
            _lock.Wait();
            try
            {
                var yPos = BeginY;
                var drawChecksum = 0;
                var performDrawByTime = true;
                var performDrawByDataChange = true;
                var activeTestsCountChanged = _activeTests.Count != _lastNumberOfTestsRunning;
                var windowWidth = 160;
                if (!_console.IsOutputRedirected)
                    windowWidth = Console.WindowWidth;

                if (_console.IsOutputRedirected)
                {
                    // if any tests have changed state based on checksum, allow a redraw
                    drawChecksum = ComputeActiveTestChecksum();
                    performDrawByTime = DateTime.Now.Subtract(_lastDrawTime).TotalMilliseconds > _drawIntervalMilliseconds;
                    performDrawByDataChange = drawChecksum != _lastDrawChecksum;
                }
                if ((performDrawByTime || performDrawByDataChange) && _activeTests.Any())
                {
                    _lastDrawChecksum = drawChecksum;
                    if (_console.IsOutputRedirected)
                        _console.WriteLine();
                    else if (activeTestsCountChanged)
                    {
                        // number of tests changed
                        var nextNumberOfTestsDrawn = Math.Min(_activeTests.Count, _configuration.MaxActiveTestsToDisplay);
                        if (nextNumberOfTestsDrawn < _lastNumberOfTestsDrawn)
                        {
                            // clear the static display if we are displaying less tests than the previous draw
                            if (!_console.IsOutputRedirected)
                                _console.ClearAtRange(0, yPos + nextNumberOfTestsDrawn, 0, yPos + 1 + _lastNumberOfLinesDrawn);
                        }
                        _lastNumberOfTestsRunning = _activeTests.Count;
                    }
                    var testNumber = 0;
                    var totalActive = _activeTests.Count(x => !x.IsQueuedForRemoval);
                    var totalPasses = _eventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Pass);
                    var totalFails = _eventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail);
                    var totalIgnored = _eventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Skipped);
                    var totalTestsProcessed = _eventLog.Count(x => x.Event.Event == EventNames.EndTest);

                    _lastNumberOfLinesDrawn = 0;
                    // write the summary of all test state
                    _console.WriteAt(ColorTextBuilder.Create
                            .Append("Tests state: ", ColorScheme.Bright)
                            .Append($"Active=", ColorScheme.Default)
                            .Append($"{totalActive} ", ColorScheme.Highlight)
                            .Append($"Pass=", ColorScheme.Default)
                            .Append($"{totalPasses} ", ColorScheme.DarkSuccess)
                            .Append($"Fail=", ColorScheme.Default)
                            .Append($"{totalFails} ", ColorScheme.DarkError)
                            .AppendIf(!_console.IsOutputRedirected, $"Ignored=", ColorScheme.Default)
                            .AppendIf(!_console.IsOutputRedirected, $"{totalIgnored} ", ColorScheme.DarkDefault)
                            .Append($"Total=", ColorScheme.Default)
                            .Append($"{totalTestsProcessed} ", ColorScheme.DarkDefault)
                            .AppendIf(_totalTestsQueued > 0, $"of ", ColorScheme.Default)
                            .AppendIf(_totalTestsQueued > 0, $"{_totalTestsQueued} ", ColorScheme.DarkDefault)
                            .AppendIf(_totalTestsQueued > 0, $"[", ColorScheme.Bright)
                            .AppendIf(_totalTestsQueued > 0, $"{((totalTestsProcessed / (double)_totalTestsQueued) * 100.0):F0}%", ColorScheme.DarkDuration)
                            .AppendIf(_totalTestsQueued > 0, $"]", ColorScheme.Bright)
                            .Append(_client.IsWaitingForConnection ? $"[waiting]" : "", ColorScheme.DarkDuration)
                            .AppendIf(!_console.IsOutputRedirected, (length) => new string(' ', Math.Max(0, windowWidth - length))),
                            0,
                            yPos,
                            DirectOutputMode.Static);
                    _lastNumberOfLinesDrawn++;
                    if (!_console.IsOutputRedirected)
                    {
                        _console.WriteAt(ColorTextBuilder.Create
                            .Append("Runtime: ", ColorScheme.Bright)
                            .Append($"{DateTime.Now.Subtract(StartTime).ToTotalElapsedTime()} ", ColorScheme.Duration)
                            .Append((length) => new string(' ', Math.Max(0, windowWidth - length))),
                            0,
                            yPos + 1,
                            DirectOutputMode.Static);
                        _lastNumberOfLinesDrawn++;
                    }
                    if (!_console.IsOutputRedirected && !string.IsNullOrEmpty(_currentFrameworkVersion))
                    {
                        _console.WriteAt(ColorTextBuilder.Create
                            .Append($"{_currentFrameworkVersion}", ColorScheme.DarkDuration)
                            .AppendIf(!_console.IsOutputRedirected, (length) => new string(' ', Math.Max(0, windowWidth - length))),
                            0, yPos + _lastNumberOfLinesDrawn, DirectOutputMode.Static);
                        _lastNumberOfLinesDrawn++;
                    }

                    // figure out how many tests we can fit on screen
                    var maxActiveTestsToDisplay = _configuration.MaxActiveTestsToDisplay;
                    if (!_console.IsOutputRedirected && maxActiveTestsToDisplay == 0)
                        maxActiveTestsToDisplay = Console.WindowHeight - yPos - 2 - _configuration.MaxFailedTestsToDisplay - 5;

                    // **************************
                    // Draw Active Tests
                    // **************************
                    IEnumerable<EventEntry> activeTestsToDisplay;
                    if (_console.IsOutputRedirected)
                    {
                        // for log file output only show running tests
                        activeTestsToDisplay = _activeTests
                            .Where(x => x.Event.TestStatus == TestStatus.Running && !x.IsQueuedForRemoval)
                            .OrderByDescending(x => x.Elapsed);
                    }
                    else
                    {
                        activeTestsToDisplay = _activeTests
                            .OrderBy(x => x.Event.TestStatus)
                            .ThenByDescending(x => x.Elapsed)
                            .Take(maxActiveTestsToDisplay);
                    }

                    foreach (var test in activeTestsToDisplay)
                    {
                        testNumber++;
                        var lifetime = DateTime.Now.Subtract(test.Event.StartTime);
                        if (test.Event.EndTime != DateTime.MinValue)
                            lifetime = test.Event.Duration;
                        var testColor = ColorScheme.Highlight;
                        var testStatus = "INVD";
                        switch (test.Event.TestStatus)
                        {
                            case TestStatus.Pass:
                                testStatus = "PASS";
                                testColor = ColorScheme.Success;
                                break;
                            case TestStatus.Fail:
                                testStatus = "FAIL";
                                testColor = ColorScheme.Error;
                                break;
                            case TestStatus.Skipped:
                                testStatus = "SKIP";
                                testColor = ColorScheme.DarkDefault;
                                break;
                            case TestStatus.Running:
                            default:
                                testStatus = "RUN ";
                                testColor = ColorScheme.Highlight;
                                break;
                        }

                        var prettyTestName = DisplayUtil.GetPrettyTestName(test.Event.TestName, ColorScheme.DarkDefault, ColorScheme.Default, ColorScheme.DarkDefault, MaxTestCaseArgumentLength);
                        // print out this test name and duration
                        _console.WriteAt(ColorTextBuilder.Create
                            // test number
                            .Append($"{testNumber}: ", ColorScheme.DarkDefault)
                            // spaced in columns
                            .AppendIf(testNumber < 10 && !_console.IsOutputRedirected, $" ")
                            // test status if not logging to file
                            .AppendIf(!_console.IsOutputRedirected, "[", ColorScheme.DarkDefault)
                            .AppendIf(!_console.IsOutputRedirected, testStatus, testColor)
                            .AppendIf(!_console.IsOutputRedirected, "] ", ColorScheme.DarkDefault)
                            // test name
                            .Append(prettyTestName)
                            // test duration
                            .Append($" {lifetime.ToTotalElapsedTime()}", ColorScheme.Duration)
                            // clear out the rest of the line
                            .AppendIf((length) => !_console.IsOutputRedirected && length < windowWidth, (length) => new string(' ', Math.Max(0, windowWidth - length)))
                            .Truncate(windowWidth),
                            0,
                            yPos + _lastNumberOfLinesDrawn,
                            DirectOutputMode.Static);
                        _lastNumberOfLinesDrawn++;
                    }
                    _lastNumberOfTestsDrawn = testNumber;

                    // **************************
                    // Draw Test Failures
                    // **************************
                    _lastNumberOfLinesDrawn += 1;
                    if (!_console.IsOutputRedirected && _configuration.MaxFailedTestsToDisplay > 0)
                    {
                        var failedTests = _eventLog
                            .Where(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail)
                            .GroupBy(x => x.Event.TestName)
                            .Select(x => x.FirstOrDefault())
                            .OrderByDescending(x => x.DateAdded)
                            .Take(_configuration.MaxFailedTestsToDisplay);
                        if (failedTests.Any())
                        {
                            _console.WriteAt(ColorTextBuilder.Create.AppendLine($"{_configuration.MaxFailedTestsToDisplay} Most Recent Failed Tests", ColorScheme.Error), 0, yPos + _lastNumberOfLinesDrawn, DirectOutputMode.Static);
                            foreach (var test in failedTests)
                            {
                                _lastNumberOfLinesDrawn++;
                                // if test shows up twice with same name, show the framework version as well
                                var lifetime = DateTime.Now.Subtract(test.Event.StartTime);
                                if (test.Event.EndTime != DateTime.MinValue)
                                    lifetime = test.Event.Duration;
                                var prettyTestName = DisplayUtil.GetPrettyTestName(test.Event.FullName, ColorScheme.DarkDefault, ColorScheme.Default, ColorScheme.DarkDefault, MaxTestCaseArgumentLength);
                                // print out this test name and duration
                                _console.WriteAt(ColorTextBuilder.Create
                                    .Append(prettyTestName)
                                    .Append($" {lifetime.ToTotalElapsedTime()}", ColorScheme.Duration)
                                    .Append(" [", ColorScheme.Bright)
                                    .Append("FAILED", ColorScheme.Error)
                                    .Append("]", ColorScheme.Bright)
                                    // clear out the rest of the line
                                    .AppendIf((length) => !_console.IsOutputRedirected && length < windowWidth, (length) => new string(' ', Math.Max(0, windowWidth - length)))
                                    .Truncate(windowWidth),
                                    0,
                                    yPos + _lastNumberOfLinesDrawn,
                                    DirectOutputMode.Static);
                            }
                        }
                    }
                }
            }
            finally
            {
                _lock.Release();
                _lastDrawTime = DateTime.Now;
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

        public void CreateReportFromHistory()
        {
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
                report.TestRunId = _testRunIds.Last();
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
                RunReports.Add(report);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Compute a checksum to see if any tests have changed state
        /// </summary>
        /// <returns></returns>
        private int ComputeActiveTestChecksum()
        {
            var hc = _activeTests.Count;
            for (var i = 0; i < _activeTests.Count; ++i)
                hc = unchecked(hc * 314159 + _activeTests[i].GetHashCode());
            return hc;
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
