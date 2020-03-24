using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NUnit.Commander.Display
{
    /// <summary>
    /// Exposes information needed for views
    /// </summary>
    public class ViewContext
    {
        public Commander Commander { get; }

        public IpcClient Client => Commander._client;
        public RunContext RunContext => Commander.RunContext;
        public int MaxTestCaseArgumentLength => Commander.MaxTestCaseArgumentLength;
        public IExtendedConsole Console => Commander._console;
        public ColorManager ColorScheme => Commander.ColorScheme;
        public bool AllowDrawActiveTests => Commander._allowDrawActiveTests;
        public SemaphoreSlim Lock => Commander._lock;
        public int BeginY => Commander.BeginY;
        public List<EventEntry> ActiveTests => Commander._activeTests;
        public List<EventEntry> ActiveTestFixtures => Commander._activeTestFixtures;
        public List<EventEntry> ActiveTestSuites => Commander._activeTestSuites;
        public List<EventEntry> EventLog => Commander._eventLog;
        public DateTime LastDrawTime
        {
            get { return Commander._lastDrawTime; }
            set { Commander._lastDrawTime = value; }
        }
        public int DrawIntervalMilliseconds => Commander._drawIntervalMilliseconds;
        public int LastDrawChecksum
        {
            get { return Commander._lastDrawChecksum; }
            set { Commander._lastDrawChecksum = value; }
        }
        public int LastNumberOfTestsRunning
        {
            get { return Commander._lastNumberOfTestsRunning; }
            set { Commander._lastNumberOfTestsRunning = value; }
        }
        public int LastNumberOfTestsDrawn
        {
            get { return Commander._lastNumberOfTestsDrawn; }
            set { Commander._lastNumberOfTestsDrawn = value; }
        }
        public int LastNumberOfLinesDrawn
        {
            get { return Commander._lastNumberOfLinesDrawn; }
            set { Commander._lastNumberOfLinesDrawn = value;  }
        }
        public ApplicationConfiguration Configuration => Commander._configuration;
        public int TotalTestsQueued => Commander._totalTestsQueued;
        public DateTime StartTime => Commander.StartTime;
        public DateTime EndTime => Commander.EndTime;
        public string CurrentFrameworkVersion => Commander._currentFrameworkVersion;

        public ViewContext(Commander commander)
        {
            Commander = commander;
        }
    }
}
