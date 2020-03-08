using CommandLine;
using CommandLine.Text;
using NUnit.Commander.Configuration;
using System.Collections.Generic;

namespace NUnit.Commander
{
    public class Options
    {
        [Option('t', "test-runner", Required = false, HelpText = "Specify which test runner to use: NUnitConsole, DotNetTest. If unspecified it will connect to any already running console runner")]
        public TestRunner? TestRunner { get; set; }

        [Option('a', "args", Required = false, HelpText = "Specify arguments to pass to nunit-console or dotnet test")]
        public string TestRunnerArguments { get; set; }

        [Option('p', "path", Required = false, HelpText = "Specify the path to the test runner")]
        public string TestRunnerPath { get; set; }

        [Option("display-output", Required = false, HelpText = "Enable displaying of test-runner output (for debugging)")]
        public bool EnableDisplayOutput { get; set; }

        [Option('l', "log", Required = false, HelpText = "Enable test logging")]
        public bool? EnableLog { get; set; }

        [Option('r', "test-reliability", Required = false, HelpText = "Enable analysis of unrelaible tests over a period of time")]
        public bool? EnableTestReliabilityAnalysis { get; set; }

        [Option('m', "max-runs", Required = false, HelpText = "Specify the number of test runs to store for reliability analysis")]
        public int? MaxTestReliabilityRuns { get; set; }

        [Option("min-runs", Required = false, HelpText = "The minimum number of history entries to analyze")]
        public int? MinTestHistoryToAnalyze { get; set; }

        [Option("ratio", Required = false, HelpText = "The minimum percentage (0.001-1.0) of history entries to analyze. Default: 0.05")]
        public double? MinTestReliabilityThreshold { get; set; }

        [Option("duration-change", Required = false, HelpText = "The minimum percentage (0.001-1.0) of duration changes to analyze. Default: 0.1")]
        public double? MaxTestDurationChange { get; set; }

        [Option("logs-path", Required = false, HelpText = "Specify the path to store logs")]
        public string LogPath { get; set; }
        
        [Option("logs-path", Required = false, HelpText = "Specify the number of slowest tests to display in report")]
        public int? SlowestTestsCount { get; set; }

        [Option('d', "display-mode", Required = false, HelpText = "Specify the display mode: LogFriendly,FullScreen")]
        public DisplayMode? DisplayMode { get; set; }

        [Option("timeout", Required = false, HelpText = "Specify the length of seconds to wait to connect")]
        public int? ConnectTimeoutSeconds { get; set; }

        [Option("generate-reports", Required = false, HelpText = "Specify which reports to generate")]
        public GenerateReportType? GenerateReportType { get; set; }

        [Usage(ApplicationAlias = @".\NUnit.Commander.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Launch commander using NUnit", new Options { TestRunner = Configuration.TestRunner.NUnitConsole, TestRunnerPath = @"C:\Path-to\nunit-console.exe", EnableLog = true });
                yield return new Example("Launch commander using dotnet test", new Options { TestRunner = Configuration.TestRunner.DotNetTest, EnableLog = true });
            }
        }
    }
}
