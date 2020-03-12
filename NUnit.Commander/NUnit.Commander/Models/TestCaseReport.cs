using NUnit.Commander.Json;
using ProtoBuf;
using System;
using System.Text.Json.Serialization;

namespace NUnit.Commander.Models
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class TestCaseReport
    {
        /// <summary>
        /// Internal NUnit test id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The .Net runtime type
        /// </summary>
        public string Runtime { get; set; }

        /// <summary>
        /// The .Net runtime full version
        /// </summary>
        public string RuntimeVersion { get; set; }

        /// <summary>
        /// Parent object id
        /// </summary>
        public string TestSuite { get; set; }

        /// <summary>
        /// Name of test
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// Full name of test
        /// </summary>
        public string FullName { get; set; }

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
        /// Total number of assertions
        /// </summary>
        public int Asserts { get; set; }

        /// <summary>
        /// True if this test was ignored
        /// </summary>
        public bool IsSkipped { get; set; }

        public TestCaseReport() { }

        public TestCaseReport(TestCaseReport dataEvent)
        {
            // clone the entire object
            if (dataEvent.Id != null)
                Id = new string(dataEvent.Id.ToCharArray());
            if (dataEvent.Runtime != null)
                Runtime = new string(dataEvent.Runtime.ToCharArray());
            if (dataEvent.RuntimeVersion != null)
                RuntimeVersion = new string(dataEvent.RuntimeVersion.ToCharArray());
            if (dataEvent.TestSuite != null)
                TestSuite = new string(dataEvent.TestSuite.ToCharArray());
            if (dataEvent.TestSuite != null)
                TestSuite = new string(dataEvent.TestSuite.ToCharArray());
            if (dataEvent.TestName != null)
                TestName = new string(dataEvent.TestName.ToCharArray());
            if (dataEvent.FullName != null)
                FullName = new string(dataEvent.FullName.ToCharArray());
            TestResult = dataEvent.TestResult;
            TestStatus = dataEvent.TestStatus;
            StartTime = dataEvent.StartTime;
            EndTime = dataEvent.EndTime;
            Duration = dataEvent.Duration;
            if (dataEvent.TestOutput != null)
                TestOutput = new string(dataEvent.TestOutput.ToCharArray());
            if (dataEvent.ErrorMessage != null)
                ErrorMessage = new string(dataEvent.ErrorMessage.ToCharArray());
            if (dataEvent.StackTrace != null)
                StackTrace = new string(dataEvent.StackTrace.ToCharArray());
            Asserts = dataEvent.Asserts;
            IsSkipped = dataEvent.IsSkipped;
        }

        public override string ToString()
        {
            return $"{TestName} - {TestStatus}";
        }
    }
}
