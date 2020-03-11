using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Extensions;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace NUnit.Commander
{
    public class Commander : ICommander, IDisposable
    {
        // how often to should poll for test event updates
        private const int DefaultPollIntervalMilliseconds = 33;
        // how often to should draw to the screen
        private const int DefaultDrawIntervalMilliseconds = 200;
        // how often to should draw to the screen when stdout is redirected
        private const int DefaultRedirectedDrawIntervalMilliseconds = 5000;
        // how long to should keep tests displayed after they have finished running
        private const int DefaultActiveTestLifetimeMilliseconds = 2000;
        // how long to should keep tests displayed after they have finished running when stdout is redirected
        private const int DefaultRedirectedActiveTestLifetimeMilliseconds = DefaultActiveTestLifetimeMilliseconds - 500;
        // position to begin drawing at
        private const int BeginY = 3;
        private readonly string[] DotNetRuntimes = new[] { "dotnet", "testhost.x86", "testhost" };
        private readonly string[] NUnitRuntimes = new[] { "nunit-console" };

        private IExtendedConsole _console;
        private IpcClient _client;
        private ManualResetEvent _closeEvent;
        private IList<EventEntry> _eventLog;
        private List<EventEntry> _activeTests;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private Thread _updateThread;
        private bool _allowDrawActiveTests = false;
        private int _lastNumberOfTestsRunning = 0;
        private int _lastNumberOfLinesDrawn = 0;
        private ApplicationConfiguration _configuration;
        private DateTime _startTime;
        private DateTime _endTime;
        private DateTime _lastDrawTime;
        private int _activeTestLifetimeMilliseconds = DefaultActiveTestLifetimeMilliseconds;
        private int _drawIntervalMilliseconds = DefaultDrawIntervalMilliseconds;
        private int _lastDrawChecksum = 0;
        private ICollection<Guid> _testRunIds = new List<Guid>();
        private ICollection<string> _frameworks = new List<string>();
        private ICollection<string> _frameworkVersions = new List<string>();
        private string _currentFramework;
        private string _currentFrameworkVersion;
        private Guid _commanderRunId = Guid.NewGuid();
        private int _totalTestsQueued = 0;

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

        public Commander(ApplicationConfiguration configuration)
        {
            _configuration = configuration;
            GenerateReportType = configuration.GenerateReportType;
            _closeEvent = new ManualResetEvent(false);
            _eventLog = new List<EventEntry>();
            _activeTests = new List<EventEntry>();
            RunReports = new List<DataEvent>();

            // start the display thread
            _updateThread = new Thread(new ThreadStart(UpdateThread));
            _updateThread.IsBackground = true;
            _updateThread.Name = "UpdateThread";
            _updateThread.Start();
            _startTime = DateTime.Now;
        }

        public Commander(ApplicationConfiguration configuration, IExtendedConsole console) : this(configuration)
        {
            _console = new ConsoleWrapper(console, configuration);
            _client = new IpcClient(configuration, console);
            _client.OnMessageReceived += IpcClient_OnMessageReceived;
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
                _eventLog.Add(e.EventEntry);
                if (!IsTestRunIdReceived(e.EventEntry.Event.TestRunId))
                    AddTestRunId(e.EventEntry.Event.TestRunId);
                if (!IsFrameworkReceived(e.EventEntry.Event.Runtime))
                    AddFramework(e.EventEntry.Event.Runtime);
                if (!IsFrameworkVersionReceived(e.EventEntry.Event.RuntimeVersion))
                    AddFrameworkVersion(e.EventEntry.Event.RuntimeVersion);
                ProcessActiveTests(e.EventEntry);
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
                    _client.Dispose();
                    // if the connection fails, write the report
                    Connect(false, (x) => { WriteReport(); x.Close(); });
                }
                else
                {
                    // NUnit does not require reconnections
                    // clear all static entries
                    // _console.ClearAtRange(0, yPos, 0, yPos + _lastNumberOfTestsRunning);
                    WriteReport();
                    Close();
                }
            }
        }

        public void Connect(bool showOutput, Action<ICommander> onFailedConnect)
        {
            _client.Connect(showOutput, (client) =>
            {
                // successful connect
                _allowDrawActiveTests = true;
            }, (client) =>
            {
                // failed connect
                onFailedConnect(this);
            });
        }

        public void WaitForClose()
        {
            _closeEvent.WaitOne();
        }

        public void Close()
        {
            _closeEvent.Set();
        }

        private void UpdateThread()
        {
            while (!_closeEvent.WaitOne(DefaultPollIntervalMilliseconds))
            {
                RemoveExpiredActiveTests();
                DisplayActiveTests();
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
                        // number of tests changed, clear the static display
                        if (!_console.IsOutputRedirected)
                            _console.ClearAtRange(0, yPos, 0, yPos + 1 + _lastNumberOfLinesDrawn);
                        _lastNumberOfTestsRunning = _activeTests.Count;
                    }
                    var testNumber = 0;
                    var totalActive = _activeTests.Count(x => !x.IsQueuedForRemoval);
                    var totalPasses = _eventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Pass);
                    var totalFails = _eventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail);
                    var totalTestsProcessed = _eventLog.Count(x => x.Event.Event == EventNames.EndTest);

                    _lastNumberOfLinesDrawn = 0;
                    // write the summary of all test state
                    _console.WriteAt(ColorTextBuilder.Create.Append("Tests state: ", Color.White)
                            .Append($"Active=", Color.Gray)
                            .Append($"{totalActive} ", Color.Green)
                            .Append($"Pass=", Color.Gray)
                            .Append($"{totalPasses} ", Color.DarkGreen)
                            .Append($"Fail=", Color.Gray)
                            .Append($"{totalFails} ", Color.DarkRed)
                            .Append($"Total=", Color.Gray)
                            .Append($"{totalTestsProcessed} ", Color.DarkSlateGray)
                            .AppendIf(_totalTestsQueued > 0, $"of ", Color.Gray)
                            .AppendIf(_totalTestsQueued > 0, $"{_totalTestsQueued} ", Color.DarkSlateGray)
                            .AppendIf(_totalTestsQueued > 0, $"[", Color.White)
                            .AppendIf(_totalTestsQueued > 0, $"{((totalTestsProcessed / (double)_totalTestsQueued) * 100.0):F0}%", Color.DarkCyan)
                            .AppendIf(_totalTestsQueued > 0, $"]", Color.White)
                            .Append(_client.IsWaitingForConnection ? $"[waiting]" : "", Color.DarkCyan)
                            .Append($"               "),
                            0,
                            yPos,
                            DirectOutputMode.Static);
                    _lastNumberOfLinesDrawn++;
                    if (!_console.IsOutputRedirected && !string.IsNullOrEmpty(_currentFrameworkVersion))
                    {
                        _console.WriteAt(ColorTextBuilder.Create.Append($"{_currentFrameworkVersion}", Color.DarkCyan), 0, yPos + _lastNumberOfLinesDrawn, DirectOutputMode.Static);
                        _lastNumberOfLinesDrawn++;
                    }
                    // write the individual tests running up to a maximum, ordered by longest running so they are visible
                    foreach (var test in _activeTests.OrderByDescending(x => x.Elapsed).Take(_configuration.MaxActiveTestsToDisplay))
                    {
                        testNumber++;
                        var lifetime = DateTime.Now.Subtract(test.Event.StartTime);
                        if (test.Event.EndTime != DateTime.MinValue)
                            lifetime = test.Event.Duration;
                        var testColor = Color.Yellow;
                        var testStatus = "RUNNING";
                        switch (test.Event.TestStatus)
                        {
                            case TestStatus.Pass:
                                testStatus = "PASS";
                                testColor = Color.Lime;
                                break;
                            case TestStatus.Fail:
                                testStatus = "FAIL";
                                testColor = Color.Red;
                                break;
                        }

                        var prettyTestName = GetPrettyTestName(test.Event.TestName);
                        // print out this test name and duration
                        _console.WriteAt(ColorTextBuilder.Create.Append($"{testNumber}: ", Color.White)
                            .Append(prettyTestName)
                            .Append($" {lifetime.ToElapsedTime()}", Color.Cyan)
                            .Append(" [", Color.White)
                            .Append(testStatus, testColor)
                            .Append("]", Color.White)
                            .AppendIf(!_console.IsOutputRedirected, "               "),
                            0,
                            yPos + _lastNumberOfLinesDrawn,
                            DirectOutputMode.Static);
                        _lastNumberOfLinesDrawn++;
                    }
                    _lastNumberOfLinesDrawn += 1;
                    if (!_console.IsOutputRedirected && _configuration.MaxFailedTestsToDisplay > 0)
                    {
                        var failedTests = _eventLog.Where(x => x.Event.Event == EventNames.EndTest && !x.Event.TestResult)
                            .OrderByDescending(x => x.DateAdded)
                            .Take(_configuration.MaxFailedTestsToDisplay);
                        if (failedTests.Any())
                        {
                            _console.WriteAt(ColorTextBuilder.Create.AppendLine($"{_configuration.MaxFailedTestsToDisplay} Most Recent Failed Tests", Color.Red), 0, yPos + _lastNumberOfLinesDrawn, DirectOutputMode.Static);
                            foreach (var test in failedTests)
                            {
                                _lastNumberOfLinesDrawn++;
                                var label = $"";
                                // if test shows up twice with same name, show the framework version as well
                                if (failedTests.Count(x => x.Event.TestName == test.Event.TestName) > 1)
                                    label = $"{test.Event.RuntimeVersion}\\";
                                var lifetime = DateTime.Now.Subtract(test.Event.StartTime);
                                if (test.Event.EndTime != DateTime.MinValue)
                                    lifetime = test.Event.Duration;
                                var prettyTestName = GetPrettyTestName(test.Event.TestName);
                                // print out this test name and duration
                                _console.WriteAt(ColorTextBuilder.Create
                                    .AppendIf(!string.IsNullOrEmpty(label), label, Color.DarkSlateGray)
                                    .Append(prettyTestName)
                                    .Append($" {lifetime.ToElapsedTime()}", Color.Cyan)
                                    .Append(" [", Color.White)
                                    .Append("FAILED", Color.Red)
                                    .Append("]", Color.White)
                                    .AppendIf(!_console.IsOutputRedirected, "               "),
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

        private ColorTextBuilder GetPrettyTestName(string testName)
        {
            var testCaseArgs = string.Empty;
            if (testName.Contains("("))
            {
                // strip the test case arguments if it won't fit on screen
                var testCaseSourceIndex = testName.IndexOf("(");
                var testCaseSourceEndIndex = testName.LastIndexOf(")");
                if (testCaseSourceIndex > 0 && testCaseSourceIndex > 0)
                {
                    var maxLength = 10;
                    testCaseArgs = testName.Substring(testCaseSourceIndex, testCaseSourceEndIndex - testCaseSourceIndex);
                    if (testCaseArgs.Length > maxLength)
                    {
                        testCaseArgs = testCaseArgs.Substring(0, maxLength) + "...";
                        if (testCaseArgs.Contains("\"")) testCaseArgs += "\"";
                    }
                    testCaseArgs += ")";
                    // remove args from test name
                    testName = testName.Substring(0, testCaseSourceIndex);
                }
            }
            if (testName.Length + testCaseArgs.Length > Console.WindowWidth - 30)
            {
                testCaseArgs = string.Empty;
                if (testName.Length + testCaseArgs.Length > Console.WindowWidth - 30)
                    testName = testName.Substring(0, Console.WindowWidth - 30) + "...";
            }

            return ColorTextBuilder.Create
                                    .Append(testName)
                                    .AppendIf(!string.IsNullOrEmpty(testCaseArgs), testCaseArgs, Color.DarkSlateGray);
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

        private void WriteReport()
        {
            _endTime = DateTime.Now;
            if (!_console.IsOutputRedirected)
            {
                _console.ClearAtRange(0, BeginY, 0, BeginY + 1 + _lastNumberOfLinesDrawn);
                _console.SetCursorPosition(0, BeginY);
            }
            var context = new ReportContext
            {
                CommanderRunId = _commanderRunId,
                StartTime = _startTime,
                EndTime = _endTime,
                FrameworkRuntimes = _frameworkVersions,
                Frameworks = _frameworks,
                TestRunIds = _testRunIds
            };
            // write the final report to the output
            var reportWriter = new ReportWriter(_console, _configuration, context);
            reportWriter.WriteFinalReport(RunReports, _eventLog);
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
                        matchingActiveTest.RemovalTime = DateTime.Now.AddMilliseconds(_activeTestLifetimeMilliseconds);
                        matchingActiveTest.Event.Duration = e.Event.Duration;
                        matchingActiveTest.Event.EndTime = e.Event.EndTime;
                        matchingActiveTest.Event.TestResult = e.Event.TestResult;
                        matchingActiveTest.Event.TestStatus = e.Event.TestStatus;
                        matchingActiveTest.Event.ErrorMessage = e.Event.ErrorMessage;
                        matchingActiveTest.Event.StackTrace = e.Event.StackTrace;
                        // Debug.WriteLine($"Set removal time to {matchingActiveTest.RemovalTime.Subtract(DateTime.Now)} for test {matchingActiveTest.Event.TestName}");
                    }
                    break;
                case EventNames.Report:
                    RunReports.Add(e.Event);
                    break;
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
                _client?.Dispose();
                _closeEvent?.Set();
                _closeEvent?.Dispose();
                _closeEvent = null;
                _client = null;
                _console?.Dispose();
            }
        }
    }
}
