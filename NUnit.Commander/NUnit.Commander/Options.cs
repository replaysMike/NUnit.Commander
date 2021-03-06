﻿using CommandLine;
using CommandLine.Text;
using NUnit.Commander.Configuration;
using System.Collections.Generic;

namespace NUnit.Commander
{
    public class Options
    {
        [Option('l', "log", Required = false, SetName = "Basic", HelpText = "Enable logging of all output")]
        public bool? EnableLog { get; set; }

        [Option("test-log", Required = false, SetName = "Basic", HelpText = "Enable a logging summary of every test")]
        public bool? EnableTestLog { get; set; }

        [Option("report-log", Required = false, SetName = "Basic", HelpText = "Enable logging of the final report only")]
        public bool? EnableReportLog { get; set; }

        [Option("logs-path", Required = false, HelpText = "Specify the path to store logs")]
        public string LogPath { get; set; }
        
        [Option("history-path", Required = false, HelpText = "Specify the path to store any historical run data")]
        public string HistoryPath { get; set; }

        [Option('d', "display-mode", Required = false, HelpText = "Specify the display mode: LogFriendly,FullScreen")]
        public DisplayMode? DisplayMode { get; set; }

        [Option("timeout", Required = false, HelpText = "Specify the length of seconds to wait to connect when using the nunit test runner")]
        public int? ConnectTimeoutSeconds { get; set; }

        [Option("dotnet-timeout", Required = false, HelpText = "Specify the length of seconds to wait to connect when using the dotnet test runner")]
        public int? DotNetConnectTimeoutSeconds { get; set; }

        [Option("max-display", Required = false, HelpText = "Specify the number of active tests to display on screen at a time. 0=auto calculate")]
        public int? MaxActiveTestsToDisplay { get; set; }

        [Option("max-failed-display", Required = false, HelpText = "Specify the number of failed tests to display on screen at a time")]
        public int? MaxFailedTestsToDisplay { get; set; } = 5;

        [Option("generate-reports", Required = false, HelpText = "Specify which reports to generate")]
        public GenerateReportType? GenerateReportType { get; set; }

        [Option("event-format", Required = false, HelpText = "Specify which event format type NUnit.Extension.TestMonitor is configured to send. Default: json")]
        public EventFormatTypes? EventFormatType { get; set; }

        [Option("slowest", Required = false, HelpText = "Specify the number of slowest tests to display in report")]
        public int? SlowestTestsCount { get; set; }

        [Option('o', "test-output", Required = false, HelpText = "Show test runner output")]
        public bool? ShowTestRunnerOutput { get; set; }

        [Option('d', "dont-prettify", Required = false, HelpText = "Specify to not prettify the error/stacktrace test output")]
        public bool? DontPrettify { get; set; }

        [Option('c', "color-scheme", Required = false, HelpText = "Specify the color scheme")]
        public ColorSchemes? ColorScheme { get; set; }

        [Option('e', "exit-on-failure", Required = false, HelpText = "Exit immediately on first test failure")]
        public bool? ExitOnFirstTestFailure { get; set; }

        // test reliability analysis

        [Option('r', "test-reliability", Required = false, SetName="Analysis", HelpText = "Enable analysis of unrelaible tests over a period of time")]
        public bool? EnableTestReliabilityAnalysis { get; set; }

        [Option('m', "max-runs", Required = false, SetName = "Analysis", HelpText = "Specify the number of test runs to store for reliability analysis")]
        public int? MaxTestReliabilityRuns { get; set; }

        [Option("min-runs", Required = false, SetName = "Analysis", HelpText = "The minimum number of history entries to analyze")]
        public int? MinTestHistoryToAnalyze { get; set; }

        [Option("ratio", Required = false, SetName = "Analysis", HelpText = "The minimum percentage (0.001-1.0) of failed tests allowed. Default: 0.1 (10%)")]
        public double? MinTestReliabilityThreshold { get; set; }

        [Option("duration-change", Required = false, SetName = "Analysis", HelpText = "The minimum percentage (0.001-1.0) of a tests duration can change before triggering failure. Default: 0.6 (60%)")]
        public double? MaxTestDurationChange { get; set; }

        [Option("min-duration", Required = false, SetName = "Analysis", HelpText = "Tests that complete less than this time will not have duration analysis checked. Default: 2000")]
        public int? MinTestMillisecondsForDurationAnalysis { get; set; }

        // command-line only options

        [Option('t', "test-runner", Required = false, SetName = "TestRunner", HelpText = "Specify which test runner to use: NUnitConsole, DotNetTest, Auto. If unspecified it will connect to any already running console runner")]
        public TestRunner? TestRunner { get; set; }

        [Option('a', "args", Required = false, SetName = "TestRunner", HelpText = "Specify arguments to pass to nunit-console or dotnet test. You can embed a \" character with \\\"")]
        public string TestRunnerArguments { get; set; }

        [Option("nunit-args", Required = false, SetName = "TestRunner", HelpText = "Specify arguments to pass to nunit-console only. You can embed a \" character with \\\"")]
        public string NUnitConsoleArguments { get; set; }

        [Option("dotnet-args", Required = false, SetName = "TestRunner", HelpText = "Specify arguments to pass to dotnet test only. You can embed a \" character with \\\"")]
        public string DotNetTestArguments { get; set; }

        [Option("test-assemblies", Required = false, SetName = "TestRunner", HelpText = "Specify a list of assemblies to test; only compatible with test-runner=Auto")]
        public string TestAssemblies { get; set; }

        [Option('p', "path", Required = false, SetName = "TestRunner", HelpText = "Specify the path to the test runner exe")]
        public string TestRunnerPath { get; set; }

        [Option("display-output", Required = false, SetName = "TestRunner", HelpText = "Enable displaying of test-runner output (for debugging)")]
        public bool EnableDisplayOutput { get; set; }

        [Option("repeat", Required = false, Default = 1, SetName = "TestRunner", HelpText = "Run tests continuously up to a specified count")]
        public int Repeat { get; set; }

        [Option("list-colors", Required = false, SetName = "TestRunner", HelpText = "List all of the colors in the color scheme")]
        public bool ListColors { get; set; }

        [Option("clear-history", Required = false, SetName = "TestRunner", HelpText = "Clear the run history database")]
        public bool ClearHistory { get; set; }

        [Option("auto-update", Required = false, SetName = "TestRunner", HelpText = "Enables automatic self-updating of Commander")]
        public bool AutoUpdate { get; set; }

        [Option("relaunch", Required = false, SetName = "TestRunner", HelpText = "Re-launch application after auto-update is complete")]
        public bool Relaunch { get; set; }

        [Usage(ApplicationAlias = @".\NUnit.Commander.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Launch commander using NUnit", new Options { TestRunner = Configuration.TestRunner.NUnitConsole, TestRunnerPath = @"C:\Path-to\nunit-console.exe", TestRunnerArguments = @"C:\Path-to\TestProject\bin\TestProject.dll C:\Path-to\TestProject2\bin\TestProject2.dll" });
                yield return new Example("Launch commander using dotnet test", new Options { TestRunner = Configuration.TestRunner.DotNetTest, TestRunnerArguments = @"C:\Path-to\Project" });
                yield return new Example("Launch commander using dotnet auto", new Options { TestRunner = Configuration.TestRunner.Auto, TestRunnerArguments = @"C:\Path-to\Project" });
            }
        }
    }
}
