using System;
using System.ComponentModel.DataAnnotations;

namespace NUnit.Commander.Configuration
{
    public class HistoryAnalysisConfiguration
    {
        /// <summary>
        /// True to enable history analysis
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Specify the number of test runs to store for reliability analysis
        /// </summary>
        public int MaxTestReliabilityRuns { get; set; } = 50;

        /// <summary>
        /// The minimum number of history entries to analyze
        /// </summary>
        public int MinTestHistoryToAnalyze { get; set; } = 5;

        /// <summary>
        /// The minimum percentage (0.001-1.0) of failed tests allowed. Default: 0.05 (5%)
        /// </summary>
        [Range(0.001, 1.0)]
        public double MinTestReliabilityThreshold { get; set; } = 0.05;

        /// <summary>
        /// The maximum percentage (0.001-1.0) of a tests duration can change before triggering failure. Default: 0.2 (20%)
        /// </summary>
        [Range(0.001, 1.0)]
        public double MaxTestDurationChange { get; set; } = 0.1;

        /// <summary>
        /// Tests that complete less than this time will not have duration analysis checked
        /// </summary>
        public int MinTestMillisecondsForDurationAnalysis { get; set; } = 1000;
    }
}
