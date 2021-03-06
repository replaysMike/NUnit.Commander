﻿namespace NUnit.Commander.IO
{
    public enum ExitCode
    {
        /// <summary>
        /// Application was updated
        /// </summary>
        ApplicationUpdated = 3,
        /// <summary>
        /// Application version requested
        /// </summary>
        VersionRequested = 2,
        /// <summary>
        /// Application argument help requested
        /// </summary>
        HelpRequested = 1,
        /// <summary>
        /// Tests passed, overall success
        /// </summary>
        Success = 0,
        /// <summary>
        /// Any tests failed
        /// </summary>
        TestsFailed = -1,
        /// <summary>
        /// Test Runner exited unexpectedly
        /// </summary>
        TestRunnerExited = -2,
        /// <summary>
        /// Invalid arguments supplied
        /// </summary>
        InvalidArguments = -3,
        /// <summary>
        /// Unhandled exception
        /// </summary>
        UnhandledException = -4,
    }
}
