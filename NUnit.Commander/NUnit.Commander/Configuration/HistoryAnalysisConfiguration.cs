using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
        /// The minimum percentage (0.001-1.0) of history entries to analyze. Default: 0.05
        /// </summary>
        [Range(0.001, 1.0)]
        public double MinTestReliabilityThreshold { get; set; } = 0.05;

        /// <summary>
        /// The minimum percentage (0.001-1.0) of duration changes to analyze. Default: 0.1
        /// </summary>
        [Range(0.001, 1.0)]
        public double MaxTestDurationChange { get; set; } = 0.1;
    }
}
