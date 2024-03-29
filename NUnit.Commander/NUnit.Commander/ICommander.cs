﻿using NUnit.Commander.Display;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;

namespace NUnit.Commander
{
    public interface ICommander
    {
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
        /// Get the run context
        /// </summary>
        RunContext RunContext { get; }

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
        /// True if commander is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// True if commander is connected to the NUnit.Extension.TestMonitor
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// True if commander is disposed
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Get the overall status of the run
        /// </summary>
        TestStatus TestStatus { get; }

        /// <summary>
        /// Wait for commander to close
        /// </summary>
        void WaitForClose();

        /// <summary>
        /// Close commander
        /// </summary>
        void Close();

        /// <summary>
        /// Generate a report context
        /// </summary>
        /// <param name="isExclusive">True if a lock must be obtained</param>
        /// <returns></returns>
        ReportContext GenerateReportContext(bool isExclusive = true);

        /// <summary>
        /// Switch to the next view
        /// </summary>
        void NextView();

        /// <summary>
        /// Set a specific view
        /// </summary>
        /// <param name="view"></param>
        void SetView(ViewPages view);
    }
}
