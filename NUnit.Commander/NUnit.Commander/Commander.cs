using AnyConsole;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace NUnit.Commander
{
    public class Commander : ICommander, IDisposable
    {
        private const int ConnectionTimeoutSeconds = 10;
        private const int MessageBufferSize = 8192;
        private const int PollIntervalMilliseconds = 100;
        private const int ActiveTestLifetimeMilliseconds = 8000;

        private ExtendedConsole _console;
        private NamedPipeClientStream _client;
        private Thread _readThread;
        private ManualResetEvent _closeEvent;
        private StringBuilder _display;
        private IList<EventEntry<DataEvent>> _eventLog;
        private List<EventEntry<DataEvent>> _activeTests;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// List of tests that are currently running
        /// </summary>
        public IReadOnlyList<EventEntry<DataEvent>> ActiveTests => new List<EventEntry<DataEvent>>(_activeTests).AsReadOnly();

        /// <summary>
        /// List of all events
        /// </summary>
        public IReadOnlyList<EventEntry<DataEvent>> EventLog => new List<EventEntry<DataEvent>>(_eventLog).AsReadOnly();

        /// <summary>
        /// Get the final run report
        /// </summary>
        public DataEvent RunReport { get; private set; }

        public Commander(ExtendedConsole console)
        {
            _console = console;
            _closeEvent = new ManualResetEvent(false);
            _display = new StringBuilder();
            _eventLog = new List<EventEntry<DataEvent>>();
            _activeTests = new List<EventEntry<DataEvent>>();
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
            while (!_closeEvent.WaitOne(PollIntervalMilliseconds))
            {
                var buffer = new byte[MessageBufferSize];
                var startTime = DateTime.Now;
                Debug.WriteLine($"BEGIN READ");
                _client.BeginRead(buffer, 0, MessageBufferSize, (ar) =>
                {
                    Debug.WriteLine($"START READ");
                    var bytesRead = _client.EndRead(ar);
                    var elapsed = DateTime.Now.Subtract(startTime);
                    Debug.WriteLine($"READ {bytesRead} bytes took {elapsed.TotalSeconds}s");
                    if (bytesRead > 0)
                    {
                        var eventStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var e = new EventEntry<DataEvent>(JsonSerializer.Deserialize<DataEvent>(eventStr));
                        Debug.WriteLine($"READ Event: {e.Event.EventName} Test: {e.Event.TestName} Duration: {e.Event.Duration}");
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
                }, null);
                RemoveExpiredActiveTests();
                DisplayActiveTests();
            }
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
                    foreach (var test in _activeTests)
                    {
                        var lifetime = DateTime.Now.Subtract(test.Event.StartTime);
                        var str = $"[{testNumber}]: {test.Event.Runtime}\\{test.Event.TestName}: {lifetime.TotalSeconds} [{GetTestStatus(test.Event)}]{Environment.NewLine}";
                        // _console.ClearAt(0, yPos + testNumber);
                        _console.WriteAt(str, 0, yPos + testNumber);
                        testNumber++;
                    }
                }
                else if (RunReport != null)
                {
                    var str = new StringBuilder();
                    str.Append($"{RunReport.TestCount} tests completed in {RunReport.Duration}{Environment.NewLine}");
                    str.Append($"Succeeded: {RunReport.Passed}{Environment.NewLine}");
                    str.Append($"Failed: {RunReport.Failed}{Environment.NewLine}");
                    _console.Clear();
                    _console.WriteLine(str.ToString());
                    Debug.WriteLine(str.ToString());
                    // signal end of run
                    // Thread.Sleep(5000);
                    // _closeEvent.Set();
                    RunReport = null;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private string GetTestStatus(DataEvent e)
        {
            if (e.EndTime > DateTime.MinValue)
                return e.TestResult ? "PASSED" : "FAILED";
            return "RUNNING";
        }

        private void RemoveExpiredActiveTests()
        {
            _lock.Wait();
            try
            {
                foreach (var test in _activeTests)
                    Debug.WriteLine($"  - Should remove {test.Event.TestName} ({test.RemovalTime.ToLongTimeString()} > {DateTime.Now.ToLongTimeString()}) = {test.RemovalTime > DateTime.Now}");
                var testsRemoved = _activeTests.RemoveAll(x => x.RemovalTime != DateTime.MinValue && x.RemovalTime < DateTime.Now);
                if (testsRemoved > 0)
                    Debug.WriteLine($"REMOVED {testsRemoved} tests");
            }
            finally
            {
                _lock.Release();
            }
        }

        private void ProcessActiveTests(EventEntry<DataEvent> e)
        {
            var eventName = Enum.Parse<EventNames>(e.Event.EventName);
            switch (eventName)
            {
                case EventNames.StartTest:
                    _activeTests.Add(e);
                    break;
                case EventNames.EndTest:
                    var matchingActiveTest = _activeTests.FirstOrDefault(x => x.Event.Id == e.Event.Id);
                    if (matchingActiveTest != null)
                    {
                        matchingActiveTest.RemovalTime = DateTime.Now.AddMilliseconds(ActiveTestLifetimeMilliseconds);
                        Debug.WriteLine($"Set removal time to {matchingActiveTest.RemovalTime.Subtract(DateTime.Now)} for test {matchingActiveTest.Event.TestName}");
                    }
                    break;
                case EventNames.Report:
                    RunReport = e.Event;
                    break;
            }
        }

        private void DebugDisplay()
        {
            Console.WriteLine(_display.ToString());
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
