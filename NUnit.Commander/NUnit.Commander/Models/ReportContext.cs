﻿using NUnit.Commander.Reporting;
using System;
using System.Collections.Generic;

namespace NUnit.Commander.Models
{
    public class ReportContext
    {
        /// <summary>
        /// The unique identifier for this test run
        /// </summary>
        public Guid CommanderRunId { get; set; }
        /// <summary>
        /// List of individual test run id's (one per test runner) found in test run
        /// </summary>
        public ICollection<Guid> TestRunIds { get; set; } = new List<Guid>();
        /// <summary>
        /// List of frameworks found in test run
        /// </summary>
        public ICollection<string> Frameworks { get; set; } = new List<string>();
        /// <summary>
        /// List of framework runtimes found in test run
        /// </summary>
        public ICollection<string> FrameworkRuntimes { get; set; } = new List<string>();
        /// <summary>
        /// List of all events during test run
        /// </summary>
        public ICollection<EventEntry> EventEntries { get; set; } = new List<EventEntry>();
        /// <summary>
        /// Start time of test run
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// Finish time of test run
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// Performance log
        /// </summary>
        public PerformanceLog PerformanceLog { get; set; }
        /// <summary>
        /// Performance overview
        /// </summary>
        public PerformanceOverview Performance { get; set; } = new PerformanceOverview();
    }
}
