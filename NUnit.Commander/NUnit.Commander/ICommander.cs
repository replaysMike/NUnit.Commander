using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;

namespace NUnit.Commander
{
    public interface ICommander
    {
        /// <summary>
        /// Connect to the Ipc server for messages
        /// </summary>
        /// <param name="showOutput">True to show output relating to connection status/failure</param>
        void Connect(bool showOutput, Action<ICommander> onFailedConnect);

        /// <summary>
        /// List of tests that are currently running
        /// </summary>
        IReadOnlyList<EventEntry> ActiveTests { get; }

        /// <summary>
        /// List of all events received by NUnit
        /// </summary>
        IReadOnlyList<EventEntry> EventLog { get; }

        /// <summary>
        /// Get the final run reports
        /// </summary>
        ICollection<DataEvent> RunReports { get; }

        /// <summary>
        /// Get the current run number
        /// </summary>
        int RunNumber { get; }

        /// <summary>
        /// Get the Commander Run Id
        /// </summary>
        Guid CommanderRunId { get; }

        /// <summary>
        /// Get the start time
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Get the end time
        /// </summary>
        DateTime EndTime { get; }

        /// <summary>
        /// Get the final report context
        /// </summary>
        ReportContext ReportContext { get; }

        /// <summary>
        /// True if commander is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Wait for commander to close
        /// </summary>
        void WaitForClose();

        /// <summary>
        /// Close commander
        /// </summary>
        void Close();
    }
}
