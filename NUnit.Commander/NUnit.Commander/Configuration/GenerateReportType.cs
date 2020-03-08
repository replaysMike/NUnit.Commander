using System;

namespace NUnit.Commander.Configuration
{
    /// <summary>
    /// The type of report to output
    /// </summary>
    [Flags]
    public enum GenerateReportType
    {
        /// <summary>
        /// A simple overview of passed/failed tests
        /// </summary>
        PassFail = 1 << 0,
        /// <summary>
        /// An overview of longest running tests
        /// </summary>
        Performance = 1 << 1,
        /// <summary>
        /// Show all test output and stacktraces
        /// </summary>
        TestOutput = 1 << 2,
        /// <summary>
        /// Show all test errors
        /// </summary>
        Errors = 1 << 3,
        /// <summary>
        /// Show all test stack traces
        /// </summary>
        StackTraces = 1 << 4,
        /// <summary>
        /// Show all test analysis
        /// </summary>
        TestAnalysis = 1 << 5,
        All = PassFail | Performance | TestOutput | Errors | StackTraces | TestAnalysis
    }
}
