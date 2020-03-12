using NUnit.Commander.Json;
using ProtoBuf;
using System;
using System.Text.Json.Serialization;

namespace NUnit.Commander.Models
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class DataEvent
    {
        /// <summary>
        /// The .Net runtime type
        /// </summary>
        public string Runtime { get; set; }

        /// <summary>
        /// The .Net runtime full version
        /// </summary>
        public string RuntimeVersion { get; set; }

        /// <summary>
        /// The unique test run id
        /// </summary>
        public Guid TestRunId { get; set; }

        /// <summary>
        /// The test runner process that is executing tests
        /// </summary>
        public string TestRunner { get; set; }

        /// <summary>
        /// Event name (StartSuite, EndSuite, StartTest, EndTest, Report)
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EventNames Event { get; set; }

        /// <summary>
        /// Internal NUnit test id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Parent object id
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// Name of test suite
        /// </summary>
        public string TestSuite { get; set; }

        /// <summary>
        /// Name of test
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// Full test name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Test type
        /// </summary>
        public string TestType { get; set; }

        /// <summary>
        /// True if test passed, false if it failed
        /// </summary>
        public bool TestResult { get; set; }

        /// <summary>
        /// Current test status
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TestStatus TestStatus { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of test/suite/run
        /// </summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Number of tests passed
        /// </summary>
        public int Passed { get; set; }

        /// <summary>
        /// Number of tests failed
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Number of warnings generated
        /// </summary>
        public int Warnings { get; set; }

        /// <summary>
        /// Number of ignored tests not run
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        /// Number of tests with an inconclusive result
        /// </summary>
        public int Inconclusive { get; set; }

        /// <summary>
        /// Total number of tests run
        /// </summary>
        public int TestCount { get; set; }

        /// <summary>
        /// Total number of assertions
        /// </summary>
        public int Asserts { get; set; }

        /// <summary>
        /// True if this test was ignored
        /// </summary>
        public bool IsSkipped { get; set; }

        /// <summary>
        /// Test output
        /// </summary>
        public string TestOutput { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Stack trace
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Full test report when all tests are completed
        /// </summary>
        public DataReport Report { get; set; }

        /// <summary>
        /// The commander run number
        /// </summary>
        [ProtoIgnore]
        public int RunNumber { get; set; }

        public DataEvent() { }

        public DataEvent(EventNames eventName)
        {
            Event = eventName;
        }

        public DataEvent(DataEvent dataEvent)
        {
            // clone the entire object
            if (dataEvent.Runtime != null)
                Runtime = new string(dataEvent.Runtime.ToCharArray());
            if (dataEvent.RuntimeVersion != null)
                RuntimeVersion = new string(dataEvent.RuntimeVersion.ToCharArray());
            Event = dataEvent.Event;
            if (dataEvent.Id != null)
                Id = new string(dataEvent.Id.ToCharArray());
            if (dataEvent.ParentId != null)
                ParentId = new string(dataEvent.ParentId.ToCharArray());
            if (dataEvent.TestSuite != null)
                TestSuite = new string(dataEvent.TestSuite.ToCharArray());
            if (dataEvent.TestName != null)
                TestName = new string(dataEvent.TestName.ToCharArray());
            if (dataEvent.FullName != null)
                FullName = new string(dataEvent.FullName.ToCharArray());
            if (dataEvent.TestType != null)
                TestType = new string(dataEvent.TestType.ToCharArray());
            TestResult = dataEvent.TestResult;
            TestStatus = dataEvent.TestStatus;
            StartTime = dataEvent.StartTime;
            EndTime = dataEvent.EndTime;
            Duration = dataEvent.Duration;
            Passed = dataEvent.Passed;
            Failed = dataEvent.Failed;
            Warnings = dataEvent.Warnings;
            Skipped = dataEvent.Skipped;
            Asserts = dataEvent.Asserts;
            IsSkipped = dataEvent.IsSkipped;
            Inconclusive = dataEvent.Inconclusive;
            TestCount = dataEvent.TestCount;
            if (dataEvent.TestOutput != null)
                TestOutput = new string(dataEvent.TestOutput.ToCharArray());
            if (dataEvent.ErrorMessage != null)
                ErrorMessage = new string(dataEvent.ErrorMessage.ToCharArray());
            if (dataEvent.StackTrace != null)
                StackTrace = new string(dataEvent.StackTrace.ToCharArray());
            if (dataEvent.Report != null)
                Report = new DataReport(dataEvent.Report);
            RunNumber = dataEvent.RunNumber;
        }

        public override string ToString()
        {
            return $"{Event} - {(TestName ?? TestSuite)} - {Id}";
        }
    }
}
