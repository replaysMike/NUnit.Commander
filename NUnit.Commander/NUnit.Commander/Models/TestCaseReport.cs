using NUnit.Commander.Json;
using System;
using System.Text.Json.Serialization;

namespace NUnit.Commander.Models
{
    public class TestCaseReport
    {
        /// <summary>
        /// Internal NUnit test id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Parent test suite
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
        [JsonConverter(typeof(TimespanConverter))]
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

        public TestCaseReport() { }
        public TestCaseReport(TestCaseReport dataEvent)
        {
            // clone the entire object
            if (dataEvent.Id != null)
                Id = new string(dataEvent.Id.ToCharArray());
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
        }
    }
}
