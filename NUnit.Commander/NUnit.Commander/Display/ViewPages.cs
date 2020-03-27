namespace NUnit.Commander.Display
{
    /// <summary>
    /// Defines the views available, and the order they are displayed
    /// </summary>
    public enum ViewPages
    {
        /// <summary>
        /// Main view of running tests
        /// </summary>
        ActiveTests = 0,
        /// <summary>
        /// View errors only
        /// </summary>
        Errors,
        /// <summary>
        /// Status overview of the current run
        /// </summary>
        RunStatus,
        /// <summary>
        /// Preview of final report
        /// </summary>
        ReportPreview
    }
}
