using NUnit.Commander.Models;

namespace NUnit.Commander.Configuration
{
    public class ApplicationConfiguration
    {
        /// <summary>
        /// Choose the display mode
        /// </summary>
        public DisplayMode DisplayMode { get; set; } = DisplayMode.FullScreen;

        /// <summary>
        /// The time in seconds to try connecting to NUnit test run
        /// </summary>
        public int ConnectTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// True to log final reports to a file
        /// </summary>
        public bool EnableLog { get; set; } = false;

        /// <summary>
        /// The event format type NUnit.Extension.TestMonitor is configured to send
        /// </summary>
        public EventFormatTypes EventFormatType { get; set; } = EventFormatTypes.Json;

        /// <summary>
        /// The reports you want to see when the run is completed
        /// </summary>
        public GenerateReportType GenerateReportType { get; set; } = GenerateReportType.All;

        /// <summary>
        /// Path to store any logs generated
        /// </summary>
        public string LogPath { get; set; }

        /// <summary>
        /// Specify the number of slowest tests to display in report
        /// </summary>
        public int SlowestTestsCount { get; set; } = 10;

        /// <summary>
        /// The number of active tests to display on screen at a time
        /// </summary>
        public int MaxActiveTestsToDisplay { get; set; } = 15;

        /// <summary>
        /// The number of failed tests to display on screen at a time
        /// </summary>
        public int MaxFailedTestsToDisplay { get; set; } = 5;

        /// <summary>
        /// How often to should draw to the screen when stdout is redirected
        /// </summary>
        public int RedirectedDrawIntervalMilliseconds { get; set; }

        /// <summary>
        /// How long to should keep tests displayed after they have finished running
        /// </summary>
        public int ActiveTestLifetimeMilliseconds { get; set; }

        /// <summary>
        /// How long to should keep tests displayed after they have finished running when stdout is redirected
        /// </summary>
        public int RedirectedActiveTestLifetimeMilliseconds { get; set; }

        /// <summary>
        /// True to always show output of the test runner
        /// </summary>
        public bool ShowTestRunnerOutput { get; set; } = false;

        /// <summary>
        /// History analysis configuration
        /// </summary>
        public HistoryAnalysisConfiguration HistoryAnalysisConfiguration { get; set; } = new HistoryAnalysisConfiguration();
    } 
}
