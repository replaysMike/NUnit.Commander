using AnyConsole;
using NUnit.Commander.Analysis;
using NUnit.Commander.Configuration;
using NUnit.Commander.Extensions;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace NUnit.Commander
{
    public class Commander : ICommander, IDisposable
    {
        private const int MessageBufferSize = 1024 * 64;
        // how often to should poll for test event updates
        private const int DefaultPollIntervalMilliseconds = 100;
        // how often to should draw to the screen
        private const int DefaultDrawIntervalMilliseconds = 42;
        // how often to should draw to the screen when stdout is redirected
        private const int DefaultRedirectedDrawIntervalMilliseconds = 5000;
        // how long to should keep tests displayed after they have finished running
        private const int DefaultActiveTestLifetimeMilliseconds = 2000;
        // how long to should keep tests displayed after they have finished running when stdout is redirected
        private const int DefaultRedirectedActiveTestLifetimeMilliseconds = DefaultActiveTestLifetimeMilliseconds - 500;
        private readonly string[] DotNetRuntimes = new[] { "dotnet", "testhost.x86", "testhost" };
        private readonly string[] NUnitRuntimes = new[] { "nunit-console" };

        private IExtendedConsole _console;
        private NamedPipeClientStream _client;
        private Thread _readThread;
        private ManualResetEvent _closeEvent;
        private ManualResetEvent _dataReadEvent;
        private StringBuilder _display;
        private IList<EventEntry> _eventLog;
        private List<EventEntry> _activeTests;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private int _lastNumberOfTestsRunning = 0;
        private ApplicationConfiguration _configuration;
        private DateTime _startTime;
        private DateTime _endTime;
        private DateTime _lastDrawTime;
        private int _activeTestLifetimeMilliseconds = DefaultActiveTestLifetimeMilliseconds;
        private int _drawIntervalMilliseconds = DefaultDrawIntervalMilliseconds;
        private int _lastDrawChecksum = 0;
        private ICollection<Guid> _testRunIds = new List<Guid>();
        private ICollection<string> _frameworks = new List<string>();
        private ICollection<string> _frameworkRuntimes = new List<string>();
        private bool _isWaitingForConnection;
        private Guid _commanderRunId = Guid.NewGuid();

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
        /// The time in seconds to try connecting to NUnit test run
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; }

        public Commander(ApplicationConfiguration configuration)
        {
            _configuration = configuration;
            GenerateReportType = configuration.GenerateReportType;
            ConnectionTimeoutSeconds = configuration.ConnectTimeoutSeconds;
            _closeEvent = new ManualResetEvent(false);
            _dataReadEvent = new ManualResetEvent(false);
            _display = new StringBuilder();
            _eventLog = new List<EventEntry>();
            _activeTests = new List<EventEntry>();
            RunReports = new List<DataEvent>();
        }

        public Commander(ApplicationConfiguration configuration, IExtendedConsole console) : this(configuration)
        {
            _console = new ConsoleWrapper(console, configuration);
            _activeTestLifetimeMilliseconds = configuration.ActiveTestLifetimeMilliseconds > 0 ? configuration.ActiveTestLifetimeMilliseconds : DefaultActiveTestLifetimeMilliseconds;
            if (_console.IsOutputRedirected)
            {
                _activeTestLifetimeMilliseconds = configuration.RedirectedActiveTestLifetimeMilliseconds > 0 ? configuration.RedirectedActiveTestLifetimeMilliseconds : DefaultRedirectedActiveTestLifetimeMilliseconds;
                _drawIntervalMilliseconds = configuration.RedirectedDrawIntervalMilliseconds > 0 ? configuration.RedirectedDrawIntervalMilliseconds : DefaultRedirectedDrawIntervalMilliseconds;
            }
        }

        public void ConnectIpcServer(bool showOutput, Action<ICommander> onFailedConnect)
        {
            var extensionName = "NUnit.Extension.TestMonitor";
            _client = new NamedPipeClientStream(".", "TestMonitorExtension", PipeDirection.InOut);
            if (showOutput)
                _console.WriteLine($"Connecting to {extensionName} (Timeout: {ConnectionTimeoutSeconds} seconds)...");
            try
            {
                _isWaitingForConnection = true;
                _client.Connect((int)TimeSpan.FromSeconds(ConnectionTimeoutSeconds).TotalMilliseconds);
                _client.ReadMode = PipeTransmissionMode.Message;
            }
            catch (TimeoutException)
            {
                _isWaitingForConnection = false;
                if (showOutput)
                {
                    _console.WriteLine(ColorTextBuilder.Create.AppendLine($"Failed to connect to {extensionName} extension within {ConnectionTimeoutSeconds} seconds.", Color.Red));
                    _console.WriteLine($"Please ensure your test runner is launched and the {extensionName} extension is correctly configured.");
                    _console.WriteLine(ColorTextBuilder.Create.Append("Try using --help, or see ").Append($"https://github.com/replaysMike/{extensionName}", Color.Blue).AppendLine(" for more details."));
                }
                onFailedConnect?.Invoke(this);
                return;
            }
            _isWaitingForConnection = false;
            if (showOutput)
                _console.WriteLine($"Connected to {extensionName}!");
            _readThread = new Thread(new ThreadStart(ReadThread));
            _readThread.IsBackground = true;
            _readThread.Name = "ReadThread";
            _readThread.Start();
        }

        public void WaitForClose()
        {
            _closeEvent.WaitOne();
        }

        public void Close()
        {
            _closeEvent.Set();
        }

        private void ReadThread()
        {
            var buffer = new byte[MessageBufferSize];
            _startTime = DateTime.Now;
            while (!_closeEvent.WaitOne(DefaultPollIntervalMilliseconds))
            {
                if (_client.CanRead && !_dataReadEvent.WaitOne(10))
                {
                    _dataReadEvent.Set();
                    _client.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReadCallback), buffer);
                }
                RemoveExpiredActiveTests();
                DisplayActiveTests();
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            var buffer = ar.AsyncState as byte[];
            var bytesRead = 0;
            try
            {
                bytesRead = _client.EndRead(ar);
            }
            catch (InvalidOperationException ex)
            {
                // server disconnected?
                Debug.WriteLine($"ERROR|{nameof(ReadCallback)}|{ex.Message} {ex.StackTrace}");
            }

            if (bytesRead > 0)
            {
                var eventStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var e = new EventEntry(JsonSerializer.Deserialize<DataEvent>(eventStr));
                Debug.WriteLine($"READ: {e.Event.Event} {(e.Event.TestName ?? e.Event.TestSuite)}");
                _lock.Wait();
                try
                {
                    _eventLog.Add(e);
                    if (!IsTestRunIdReceived(e.Event.TestRunId))
                        AddTestRunId(e.Event.TestRunId);
                    if (!IsFrameworkReceived(e.Event.Runtime))
                        AddFramework(e.Event.Runtime);
                    if (!IsFrameworkVersionReceived(e.Event.RuntimeVersion))
                        AddFrameworkVersion(e.Event.RuntimeVersion);
                    ProcessActiveTests(e);
                }
                finally
                {
                    _lock.Release();
                }

                if (e.Event.Event == EventNames.Report)
                {
                    // when we receive a report, we need to reconnect to the IpcServer as DotNetTest behaves differently than
                    // NUnit in this manner. It will run tests in new processes, so we need to reconnect and see if more tests are running.
                    if (DotNetRuntimes.Contains(e.Event.TestRunner))
                    {
                        // disconnect from the server, and wait for a new connection to appear
                        Debug.WriteLine($"Waiting for another server connection...");
                        _client.Dispose();
                        // if the connection fails, write the report
                        ConnectIpcServer(false, (x) => { WriteReport(); x.Close(); });
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
            // signal ready to read more data
            _dataReadEvent.Reset();
        }

        private void DisplayActiveTests()
        {
            _lock.Wait();
            try
            {
                var yPos = 5;
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
                            _console.ClearAtRange(0, yPos, 0, yPos + 1 + _lastNumberOfTestsRunning);
                        _lastNumberOfTestsRunning = _activeTests.Count;
                    }
                    var testNumber = 0;
                    // write the summary of all test state
                    _console.WriteAt(ColorTextBuilder.Create.Append("Tests state: ")
                            .Append($"Active=", Color.Gray)
                            .Append($"{_activeTests.Count(x => !x.IsQueuedForRemoval)} ", Color.Green)
                            .Append($"Pass=", Color.Gray)
                            .Append($"{_eventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Pass)} ", Color.DarkGreen)
                            .Append($"Fail=", Color.Gray)
                            .Append($"{_eventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail)} ", Color.DarkRed)
                            .Append($"Total=", Color.Gray)
                            .Append($"{_eventLog.Count(x => x.Event.Event == EventNames.EndTest)} ", Color.DarkGray)
                            .Append(_isWaitingForConnection ? $"[waiting]" : "", Color.DarkCyan)
                            .Append($"               "),
                            0,
                            yPos,
                            DirectOutputMode.Static);
                    // write the individual tests running up to a maximum, ordered by longest running so they are visible
                    foreach (var test in _activeTests.OrderByDescending(x => x.Elapsed).Take(_configuration.MaxActiveTestsToDisplay))
                    {
                        testNumber++;
                        var lifetime = DateTime.Now.Subtract(test.Event.StartTime);
                        if (test.Event.EndTime != DateTime.MinValue)
                            lifetime = test.Event.Duration;
                        var str = $"{testNumber}: {test.Event.TestName}: ";
                        var testColor = Color.Yellow;
                        var testStatus = "RUNNING";
                        switch (test.Event.TestStatus)
                        {
                            case TestStatus.Pass:
                                testStatus = "PASS";
                                testColor = Color.Green;
                                break;
                            case TestStatus.Fail:
                                testStatus = "FAIL";
                                testColor = Color.Red;
                                break;
                        }

                        // print out this test name and duration
                        _console.WriteAt(ColorTextBuilder.Create.Append(str)
                            .Append(lifetime.ToElapsedTime(), Color.Cyan)
                            .Append(" [", Color.White)
                            .Append(testStatus, testColor)
                            .Append("]", Color.White)
                            .Append("               "),
                            0,
                            yPos + testNumber,
                            DirectOutputMode.Static);
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
                _frameworks.Add(framework);
        }

        private bool IsFrameworkVersionReceived(string frameworkVersion)
        {
            return _frameworkRuntimes.Contains(frameworkVersion);
        }

        private void AddFrameworkVersion(string frameworkVersion)
        {
            if (!string.IsNullOrEmpty(frameworkVersion))
                _frameworkRuntimes.Add(frameworkVersion);
        }

        private void WriteReport()
        {
            _endTime = DateTime.Now;
            var context = new ReportContext { 
                CommanderRunId = _commanderRunId,
                StartTime = _startTime,
                EndTime = _endTime,
                FrameworkRuntimes = _frameworkRuntimes,
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
                _closeEvent?.Set();
                if (_readThread?.Join(5 * 1000) == false)
                    _readThread.Abort();
                _closeEvent?.Dispose();
                _readThread = null;
                _closeEvent = null;
                _console?.Dispose();
            }
        }
    }
}
