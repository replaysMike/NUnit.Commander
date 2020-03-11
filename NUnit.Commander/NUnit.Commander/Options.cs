using CommandLine;
using CommandLine.Text;
using NUnit.Commander.Configuration;
using System.Collections.Generic;

namespace NUnit.Commander
{
    public class Options
    {
        [Option('l', "log", Required = false, SetName = "Basic", HelpText = "Enable test logging")]
        public bool? EnableLog { get; set; }

        [Option("logs-path", Required = false, HelpText = "Specify the path to store logs")]
        public string LogPath { get; set; }

        [Option('d', "display-mode", Required = false, HelpText = "Specify the display mode: LogFriendly,FullScreen")]
        public DisplayMode? DisplayMode { get; set; }

        [Option("timeout", Required = false, HelpText = "Specify the length of seconds to wait to connect")]
        public int? ConnectTimeoutSeconds { get; set; }

        [Option("max-display", Required = false, HelpText = "Specify the number of active tests to display on screen at a time")]
        public int? MaxActiveTestsToDisplay { get; set; }

        [Option("max-failed-display", Required = false, HelpText = "Specify the number of failed tests to display on screen at a time")]
        public int? MaxFailedTestsToDisplay { get; set; } = 5;

        [Option("generate-reports", Required = false, HelpText = "Specify which reports to generate")]
        public GenerateReportType? GenerateReportType { get; set; }

        [Option("slowest", Required = false, HelpText = "Specify the number of slowest tests to display in report")]
        public int? SlowestTestsCount { get; set; }

        [Option('o', "test-output", Required = false, HelpText = "Show test runner output")]
        public bool? ShowTestRunnerOutput { get; set; }

        // test reliability analysis

        [Option('r', "test-reliability", Required = false, SetName="Analysis", HelpText = "Enable analysis of unrelaible tests over a period of time")]
        public bool? EnableTestReliabilityAnalysis { get; set; }

        [Option('m', "max-runs", Required = false, SetName = "Analysis", HelpText = "Specify the number of test runs to store for reliability analysis")]
        public int? MaxTestReliabilityRuns { get; set; }

        [Option("min-runs", Required = false, SetName = "Analysis", HelpText = "The minimum number of history entries to analyze")]
        public int? MinTestHistoryToAnalyze { get; set; }

        [Option("ratio", Required = false, SetName = "Analysis", HelpText = "The minimum percentage (0.001-1.0) of history entries to analyze. Default: 0.05")]
        public double? MinTestReliabilityThreshold { get; set; }

        [Option("duration-change", Required = false, SetName = "Analysis", HelpText = "The minimum percentage (0.001-1.0) of duration changes to analyze. Default: 0.1")]
        public double? MaxTestDurationChange { get; set; }

        // command-line only options

        [Option('t', "test-runner", Required = false, SetName = "TestRunner", HelpText = "Specify which test runner to use: NUnitConsole, DotNetTest. If unspecified it will connect to any already running console runner")]
        public TestRunner? TestRunner { get; set; }

        [Option('a', "args", Required = false, SetName = "TestRunner", HelpText = "Specify arguments to pass to nunit-console or dotnet test. You can embed a \" character with \\\"")]
        public string TestRunnerArguments { get; set; }

        [Option('p', "path", Required = false, SetName = "TestRunner", HelpText = "Specify the path to the test runner exe")]
        public string TestRunnerPath { get; set; }

        [Option("display-output", Required = false, SetName = "TestRunner", HelpText = "Enable displaying of test-runner output (for debugging)")]
        public bool EnableDisplayOutput { get; set; }

        [Usage(ApplicationAlias = @".\NUnit.Commander.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Launch commander using NUnit", new Options { TestRunner = Configuration.TestRunner.NUnitConsole, TestRunnerPath = @"C:\Path-to\nunit-console.exe", TestRunnerArguments = @"C:\Path-to\TestProject\bin\TestProject.dll C:\Path-to\TestProject2\bin\TestProject2.dll" });
                yield return new Example("Launch commander using dotnet test", new Options { TestRunner = Configuration.TestRunner.DotNetTest, TestRunnerArguments = @"C:\Path-to\Project" });
            }
        }
    }
}
