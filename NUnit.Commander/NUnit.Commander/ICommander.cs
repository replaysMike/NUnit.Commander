using NUnit.Commander.Models;
using System.Collections.Generic;

namespace NUnit.Commander
{
    public interface ICommander
    {
        /// <summary>
        /// Connect to the Ipc server for messages
        /// </summary>
        void ConnectIpcServer();

        /// <summary>
        /// List of tests that are currently running
        /// </summary>
        IReadOnlyList<EventEntry> ActiveTests { get; }

        /// <summary>
        /// List of all events received by NUnit
        /// </summary>
        IReadOnlyList<EventEntry> EventLog { get; }

        /// <summary>
        /// Get the final run report
        /// </summary>
        DataEvent RunReport { get; }
    }
}
