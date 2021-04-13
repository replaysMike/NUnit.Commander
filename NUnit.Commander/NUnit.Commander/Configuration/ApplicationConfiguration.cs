namespace NUnit.Commander.Configuration
{
    public class ApplicationConfiguration
    {
        /// <summary>
        /// Choose the display mode
        /// </summary>
        public DisplayMode DisplayMode { get; set; } = DisplayMode.FullScreen;

        /// <summary>
        /// The time in seconds to try connecting to NUnit test run when using the NUnit Console test runner
        /// </summary>
        public int ConnectTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// The time in seconds to try connecting to NUnit test run when using the Dotnet test runner
        /// </summary>
        public int DotNetConnectTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// True to log final reports to a file
        /// </summary>
        public bool EnableLog { get; set; } = false;

        /// <summary>
        /// True to log a summary of every test
        /// </summary>
        public bool EnableTestLog { get; set; } = false;

        /// <summary>
        /// True to log the final report
        /// </summary>
        public bool EnableReportLog { get; set; } = false;

        /// <summary>
        /// True to skip prettify the test result output. Default: false
        /// </summary>
        public bool DontPrettify { get; set; } = false;

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
        /// Path to store any historical run data
        /// </summary>
        public string HistoryPath { get; set; }

        /// <summary>
        /// Specify the number of slowest tests to display in report
        /// </summary>
        public int SlowestTestsCount { get; set; } = 10;

        /// <summary>
        /// The number of active tests to display on screen at a time. 0=auto calculate
        /// </summary>
        public int MaxActiveTestsToDisplay { get; set; } = 0;

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
        /// Specify the color scheme
        /// </summary>
        public ColorSchemes ColorScheme { get; set; } = ColorSchemes.Default;

        /// <summary>
        /// Exit immediately on first test failure
        /// </summary>
        public bool ExitOnFirstTestFailure { get; set; } = false;

        /// <summary>
        /// History analysis configuration
        /// </summary>
        public HistoryAnalysisConfiguration HistoryAnalysisConfiguration { get; set; } = new HistoryAnalysisConfiguration();

        /// <summary>
        /// Display configuration
        /// </summary>
        internal DisplayConfiguration DisplayConfiguration { get; set; } = new DisplayConfiguration();
    } 
}
