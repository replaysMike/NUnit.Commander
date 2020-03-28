using NUnit.Commander.IO;
using NUnit.Commander.Reporting;
using System.Collections.Generic;

namespace NUnit.Commander.Models
{
    public class RunContext
    {
        /// <summary>
        /// List of individual test runs
        /// </summary>
        public Dictionary<ReportContext, ICollection<DataEvent>> Runs { get; set; } = new Dictionary<ReportContext, ICollection<DataEvent>>();

        /// <summary>
        /// History analysis report
        /// </summary>
        public HistoryReport HistoryReport { get; set; }

        /// <summary>
        /// Database provider for test history data
        /// </summary>
        public TestHistoryDatabaseProvider TestHistoryDatabaseProvider { get; set; }

        /// <summary>
        /// Performance counters for total run
        /// </summary>
        public PerformanceCounters PerformanceCounters { get; set; } = new PerformanceCounters();
    }
}
