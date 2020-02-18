using NUnit.Commander.Json;
using System;
using System.Text.Json.Serialization;

namespace NUnit.Commander.Models
{
    public class DataEvent
    {
        public string Runtime { get; set; }
        public string EventName { get; set; }
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string TestSuite { get; set; }
        public string TestName { get; set; }
        public string FullName { get; set; }
        public bool TestResult { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [JsonConverter(typeof(TimespanConverter))]
        public TimeSpan Duration { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Warnings { get; set; }
        public int Skipped { get; set; }
        public int Inconclusive { get; set; }
        public int TestCount { get; set; }
        public DataReport Report { get; set; }
    }

    public class DataReport
    {
        public int TotalTests { get; set; }
    }
}
