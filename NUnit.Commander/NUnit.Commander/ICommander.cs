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
        void ConnectIpcServer(bool showOutput, Action<ICommander> onFailedConnect);

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
        /// Wait for commander to close
        /// </summary>
        void WaitForClose();

        /// <summary>
        /// Close commander
        /// </summary>
        void Close();
    }
}
