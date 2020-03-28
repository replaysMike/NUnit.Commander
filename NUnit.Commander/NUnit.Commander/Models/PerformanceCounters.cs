using System.Diagnostics;

namespace NUnit.Commander.Models
{
    /// <summary>
    /// Performance counters
    /// </summary>
    public class PerformanceCounters
    {
        /// <summary>
        /// Cpu performance counter
        /// </summary>
        public PerformanceCounter CpuCounter { get; set; }

        /// <summary>
        /// Disk/hard drive performance counter
        /// </summary>
        public PerformanceCounter DiskCounter { get; set; }
    }
}
