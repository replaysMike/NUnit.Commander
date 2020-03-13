using NUnit.Commander.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace NUnit.Commander.Models
{
    public class RunContext
    {
        public Dictionary<ReportContext, ICollection<DataEvent>> Runs { get; set; } = new Dictionary<ReportContext, ICollection<DataEvent>>();
        public HistoryReport HistoryReport { get; set; }
        public TestHistoryDatabaseProvider TestHistoryDatabaseProvider { get; set; }
        public PerformanceCounters PerformanceCounters { get; set; } = new PerformanceCounters();
    }

    public class PerformanceCounters
    {
        public PerformanceCounter CpuCounter { get; set; }
        public PerformanceCounter DiskCounter { get; set; }
    }
}
