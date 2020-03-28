namespace NUnit.Commander.Models
{
    /// <summary>
    /// Performance summary overview
    /// </summary>
    public class PerformanceOverview
    {
        public double PeakCpuUsed { get; set; }
        public double PeakMemoryUsed { get; set; }
        public double PeakDiskTime { get; set; }
        public double PeakTestConcurrency { get; set; }
        public double PeakTestFixtureConcurrency { get; set; }
        public double PeakAssemblyConcurrency { get; set; }
        public double MedianMemoryUsed { get; set; }
        public double MedianCpuUsed { get; set; }
        public double MedianDiskTime { get; set; }
        public double MedianTestConcurrency { get; set; }
        public double MedianTestFixtureConcurrency { get; set; }
        public double MedianAssemblyConcurrency { get; set; }
    }
}
