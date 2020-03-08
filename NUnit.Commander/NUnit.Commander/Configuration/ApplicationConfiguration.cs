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
        public int ConnectTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// True to log final reports to a file
        /// </summary>
        public bool EnableLog { get; set; } = false;

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
        public int SlowestTestsCount { get; set; }

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
        /// History analysis configuration
        /// </summary>
        public HistoryAnalysisConfiguration HistoryAnalysisConfiguration { get; set; } = new HistoryAnalysisConfiguration();
    } 
}
