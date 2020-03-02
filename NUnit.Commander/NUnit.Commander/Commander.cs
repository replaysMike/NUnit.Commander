using AnyConsole;
using NUnit.Commander.Extensions;
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
        private const int ConnectionTimeoutSeconds = 30;
        private const int MessageBufferSize = 1024 * 64;
        private const int PollIntervalMilliseconds = 100;
        private const int ActiveTestLifetimeMilliseconds = 2000;

        private ExtendedConsole _console;
        private NamedPipeClientStream _client;
        private Thread _readThread;
        private ManualResetEvent _closeEvent;
        private ManualResetEvent _dataReadEvent;
        private StringBuilder _display;
        private IList<EventEntry> _eventLog;
        private List<EventEntry> _activeTests;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private int _lastNumberOfTestsRunning = 0;

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
        public GenerateReportType GenerateReportType { get; set; } = GenerateReportType.All;

        /// <summary>
        /// Get the final run report
        /// </summary>
        public DataEvent RunReport { get; private set; }

        public Commander(ExtendedConsole console)
        {
            _console = console;
            _closeEvent = new ManualResetEvent(false);
            _dataReadEvent = new ManualResetEvent(false);
            _display = new StringBuilder();
            _eventLog = new List<EventEntry>();
            _activeTests = new List<EventEntry>();
        }

        public void ConnectIpcServer()
        {
            _client = new NamedPipeClientStream(".", "TestMonitorExtension", PipeDirection.InOut);
            _console.WriteLine("Connecting...");
            try
            {
                _client.Connect((int)TimeSpan.FromSeconds(ConnectionTimeoutSeconds).TotalMilliseconds);
                _client.ReadMode = PipeTransmissionMode.Message;
            }
            catch (TimeoutException)
            {
                _console.WriteLine("Failed to connect to NUnit TestMonitor extension.");
                _closeEvent.Set();
                return;
            }
            _console.WriteLine("Connected!");
            _readThread = new Thread(new ThreadStart(ReadThread));
            _readThread.IsBackground = true;
            _readThread.Name = "ReadThread";
            _readThread.Start();
        }

        public void WaitForCompletion()
        {
            _closeEvent.WaitOne();
        }

        private void ReadThread()
        {
            var buffer = new byte[MessageBufferSize];
            while (!_closeEvent.WaitOne(PollIntervalMilliseconds))
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
            var bytesRead = _client.EndRead(ar);

            if (bytesRead > 0)
            {
                var eventStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var e = new EventEntry(JsonSerializer.Deserialize<DataEvent>(eventStr));
                Debug.WriteLine($"READ: {e.Event.Event} {(e.Event.TestName ?? e.Event.TestSuite)}");
                _lock.Wait();
                try
                {
                    _eventLog.Add(e);
                    ProcessActiveTests(e);
                }
                finally
                {
                    _lock.Release();
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
                if (_activeTests.Any())
                {
                    var testNumber = 0;
                    if (_activeTests.Count != _lastNumberOfTestsRunning)
                    {
                        // number of tests changed, clear the display
                        var entriesCleared = _console.ClearAtRange(0, yPos, 0, yPos + _lastNumberOfTestsRunning);
                        _lastNumberOfTestsRunning = _activeTests.Count;
                    }
                    foreach (var test in _activeTests)
                    {
                        var lifetime = DateTime.Now.Subtract(test.Event.StartTime);
                        if (test.Event.EndTime != DateTime.MinValue)
                            lifetime = test.Event.Duration;
                        var str = $"[{testNumber}]: {test.Event.Runtime}\\{test.Event.TestName}: {lifetime.ToElapsedTime()}";
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
                        
                        _console.WriteAt(ColorTextBuilder.Create.Append(str).Append(" [", Color.White).Append(testStatus, testColor).Append("]    ", Color.White), 0, yPos + testNumber, DirectOutputMode.Static);
                        testNumber++;
                    }
                }
                else if (RunReport != null)
                {
                    // clear all static entries
                    _console.ClearAtRange(0, yPos, 0, yPos + _lastNumberOfTestsRunning);

                    WriteReport();
                    _closeEvent.Set();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private void WriteReport()
        {
            var passFail = new StringBuilder();
            if (GenerateReportType.HasFlag(GenerateReportType.PassFail))
            {
                passFail.AppendLine($"{RunReport.TestCount} tests completed in {RunReport.Duration.ToTotalElapsedTime()}");
                passFail.AppendLine($"Succeeded: {RunReport.Passed}");
                passFail.AppendLine($"Failed: {RunReport.Failed}");
                passFail.AppendLine(Environment.NewLine);
                passFail.AppendLine(Environment.NewLine);
            }

            var performance = new StringBuilder();
            if (GenerateReportType.HasFlag(GenerateReportType.Performance))
            {
                performance.AppendLine("Top 10 slowest tests");
                performance.AppendLine("=======================");
                var slowestTests = _eventLog
                    .Where(x => x.Event.Event == EventNames.EndTest)
                    .OrderByDescending(x => x.Event.Duration)
                    .Take(10);
                foreach (var test in slowestTests)
                    performance.AppendLine($"{test.Event.FullName} : [{test.Event.Duration.ToElapsedTime()}]");
                performance.AppendLine(Environment.NewLine);
                performance.AppendLine(Environment.NewLine);
            }

            // output test errors
            var testOutput = new StringBuilder();
            var showErrors = GenerateReportType.HasFlag(GenerateReportType.Errors);
            var showStackTraces = GenerateReportType.HasFlag(GenerateReportType.StackTraces);
            var showTestOutput = GenerateReportType.HasFlag(GenerateReportType.TestOutput);
            if (showErrors || showStackTraces || showTestOutput)
            {
                if(RunReport.TestStatus == TestStatus.Fail)
                {
                    testOutput.AppendLine("FAILED TESTS:");
                    testOutput.AppendLine(Environment.NewLine);
                }

                foreach (var test in RunReport.Report.TestReports.Where(x => !x.TestResult))
                {
                    testOutput.AppendLine($"FAILED [{test.Id}]:");
                    testOutput.AppendLine($"{test.TestName}");
                    testOutput.AppendLine($"========================================");
                    testOutput.AppendLine($"Duration: {test.Duration.ToElapsedTime()}");
                    if (showErrors)
                        testOutput.AppendLine($"Error: {test.ErrorMessage}");
                    if (showStackTraces)
                        testOutput.AppendLine($"Stack Trace: {test.StackTrace}");
                    if (showTestOutput)
                        testOutput.AppendLine($"Test Output: {test.TestOutput}");
                    testOutput.AppendLine(Environment.NewLine);
                    testOutput.AppendLine(Environment.NewLine);
                }
            }

            switch (RunReport.TestStatus)
            {
                case TestStatus.Pass:
                    _console.WriteAscii("PASSED");
                    break;
                case TestStatus.Fail:
                    _console.WriteAscii("FAILED");
                    break;
            }

            if (passFail.Length > 0)
                _console.WriteLine(passFail);
            if (performance.Length > 0)
                _console.WriteLine(performance);

            if (testOutput.Length > 0)
                _console.WriteLine(testOutput);
        }

        private void RemoveExpiredActiveTests()
        {
            _lock.Wait();
            try
            {
                var testsRemoved = _activeTests.RemoveAll(x => x.RemovalTime != DateTime.MinValue && x.RemovalTime < DateTime.Now);
                if (testsRemoved > 0)
                {
                    Debug.WriteLine($"REMOVED {testsRemoved} tests");
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
                        matchingActiveTest.RemovalTime = DateTime.Now.AddMilliseconds(ActiveTestLifetimeMilliseconds);
                        matchingActiveTest.Event.Duration = e.Event.Duration;
                        matchingActiveTest.Event.EndTime = e.Event.EndTime;
                        matchingActiveTest.Event.TestResult = e.Event.TestResult;
                        matchingActiveTest.Event.TestStatus = e.Event.TestStatus;
                        matchingActiveTest.Event.ErrorMessage = e.Event.ErrorMessage;
                        matchingActiveTest.Event.StackTrace = e.Event.StackTrace;
                        Debug.WriteLine($"Set removal time to {matchingActiveTest.RemovalTime.Subtract(DateTime.Now)} for test {matchingActiveTest.Event.TestName}");
                    }
                    break;
                case EventNames.Report:
                    RunReport = e.Event;
                    break;
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
                _closeEvent?.Set();
                if (_readThread?.Join(5 * 1000) == false)
                    _readThread.Abort();
                _closeEvent?.Dispose();
                _readThread = null;
                _closeEvent = null;
            }
        }
    }
}
