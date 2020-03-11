using AnyConsole;
using CommandLine;
using NUnit.Commander.Configuration;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Console = Colorful.Console;

namespace NUnit.Commander
{
    class Program
    {
        static void Main(string[] args)
        {
            var configProvider = new ConfigurationProvider();
            var configuration = configProvider.LoadConfiguration();
            var config = configProvider.Get<ApplicationConfiguration>(configuration);

            var parser = new Parser(c =>
            {
                c.CaseSensitive = false;
                c.HelpWriter = Console.Error;
            });
            parser.ParseArguments<Options>(args)
                .WithParsed<Options>(o => Start(o, config))
                .WithNotParsed<Options>(errors => ArgsParsingError(errors));
        }

        private static void Start(Options options, ApplicationConfiguration config)
        {
            options = ProcessQuotedParameters(options);

            // override any configuration options via commandline
            if (options.EnableLog.HasValue)
                config.EnableLog = options.EnableLog.Value;
            if (options.EnableTestReliabilityAnalysis.HasValue)
                config.HistoryAnalysisConfiguration.Enabled = options.EnableTestReliabilityAnalysis.Value;
            if (options.MaxTestReliabilityRuns.HasValue)
                config.HistoryAnalysisConfiguration.MaxTestReliabilityRuns = options.MaxTestReliabilityRuns.Value;
            if (options.MinTestHistoryToAnalyze.HasValue)
                config.HistoryAnalysisConfiguration.MinTestHistoryToAnalyze = options.MinTestHistoryToAnalyze.Value;
            if (options.MinTestReliabilityThreshold.HasValue)
                config.HistoryAnalysisConfiguration.MinTestReliabilityThreshold = options.MinTestReliabilityThreshold.Value;
            if (options.MaxTestDurationChange.HasValue)
                config.HistoryAnalysisConfiguration.MaxTestDurationChange = options.MaxTestDurationChange.Value;
            if (options.MinTestMillisecondsForDurationAnalysis.HasValue)
                config.HistoryAnalysisConfiguration.MinTestMillisecondsForDurationAnalysis = options.MinTestMillisecondsForDurationAnalysis.Value;
            if (options.DisplayMode.HasValue)
                config.DisplayMode = options.DisplayMode.Value;
            if (options.ConnectTimeoutSeconds.HasValue)
                config.ConnectTimeoutSeconds = options.ConnectTimeoutSeconds.Value;
            if (options.MaxActiveTestsToDisplay.HasValue)
                config.MaxActiveTestsToDisplay = options.MaxActiveTestsToDisplay.Value;
            if (options.MaxFailedTestsToDisplay.HasValue)
                config.MaxFailedTestsToDisplay = options.MaxFailedTestsToDisplay.Value;
            if (options.GenerateReportType.HasValue)
                config.GenerateReportType = options.GenerateReportType.Value;
            if (options.SlowestTestsCount.HasValue)
                config.SlowestTestsCount = options.SlowestTestsCount.Value;
            if (options.ShowTestRunnerOutput.HasValue)
                config.ShowTestRunnerOutput = options.ShowTestRunnerOutput.Value;
            if (!string.IsNullOrEmpty(options.LogPath))
                config.LogPath = options.LogPath;

            var testRunnerSuccess = true;
            TestRunnerLauncher launcher = null;
            var runNumber = 0;
            var runContext = new RunContext();
            while (runNumber < options.Repeat)
            {
                runNumber++;
                if (options.TestRunner.HasValue)
                {
                    // launch test runner in another process if asked
                    launcher = new TestRunnerLauncher(options);
                    testRunnerSuccess = launcher.StartTestRunner();
                }

                var commanderIsSuccess = false;
                if (testRunnerSuccess)
                {
                    // blocking
                    switch (config.DisplayMode)
                    {
                        case DisplayMode.LogFriendly:
                            commanderIsSuccess = RunLogFriendly(options, config, runNumber, runContext);
                            break;
                        case DisplayMode.FullScreen:
                            commanderIsSuccess = RunFullScreen(options, config, runNumber, runContext);
                            break;
                    }

                    if (launcher != null)
                    {
                        //Console.Error.WriteLine($"Exit code: {launcher.ExitCode}");
                        //Console.Error.WriteLine($"OUTPUT: {launcher.ConsoleOutput}");
                        //Console.Error.WriteLine($"ERRORS: {launcher.ConsoleError}");
                        ParseConsoleRunnerOutput(commanderIsSuccess, options, config, launcher);
                        launcher.Dispose();
                    }
                }
            }
        }
        private static Options ProcessQuotedParameters(Options options)
        {
            if (options.TestRunnerArguments.Contains(@"\"""))
            {
                // replace \" with "
                options.TestRunnerArguments = options.TestRunnerArguments.Replace("\\\"", "\"");
            }
            return options;
        }

        private static void ParseConsoleRunnerOutput(bool isSuccess, Options options, ApplicationConfiguration config, TestRunnerLauncher launcher)
        {
            launcher.WaitForExit();
            var output = launcher.ConsoleOutput;
            var error = launcher.ConsoleError;
            var exitCode = launcher.ExitCode;
            switch (options.TestRunner)
            {
                case TestRunner.NUnitConsole:
                    if ((exitCode < 0 && !isSuccess) || config.ShowTestRunnerOutput)
                    {
                        var startErrorsIndex = output.IndexOf("Errors, Failures and Warnings");
                        if (startErrorsIndex >= 0)
                        {
                            // show errors output
                            var endErrorsIndex = output.IndexOf("Test Run Summary", startErrorsIndex);
                            var errors = output.Substring(startErrorsIndex, endErrorsIndex - startErrorsIndex);
                            Console.ForegroundColor = Color.DarkRed;
                            Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:");
                            Console.ForegroundColor = Color.FromArgb(50, 0, 0);
                            Console.WriteLine("============================");
                            Console.ForegroundColor = Color.Red;
                            Console.WriteLine(errors);
                            Console.ForegroundColor = Color.Gray;
                        }
                        else
                        {
                            // show entire output
                            Console.ForegroundColor = Color.DarkRed;
                            Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:");
                            Console.ForegroundColor = Color.FromArgb(50, 0, 0);
                            Console.WriteLine("============================");
                            Console.ForegroundColor = Color.Red;
                            Console.WriteLine(output);
                            Console.ForegroundColor = Color.Gray;
                        }
                    }
                    break;
                case TestRunner.DotNetTest:
                    if (config.ShowTestRunnerOutput || (!isSuccess && !string.IsNullOrEmpty(error) && error != Environment.NewLine && !error.Contains("Test Run Failed.")))
                    {
                        Console.ForegroundColor = Color.DarkRed;
                        Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:");
                        Console.ForegroundColor = Color.FromArgb(50, 0, 0);
                        Console.WriteLine("============================");
                        Console.ForegroundColor = Color.Red;
                        Console.WriteLine(error);
                        Console.ForegroundColor = Color.Gray;
                    }
                    else if (config.ShowTestRunnerOutput || (!isSuccess && output.Contains("MSBUILD : error ")))
                    {
                        Console.ForegroundColor = Color.DarkRed;
                        Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:");
                        Console.ForegroundColor = Color.FromArgb(50, 0, 0);
                        Console.WriteLine("============================");
                        Console.ForegroundColor = Color.Red;
                        Console.WriteLine(output);
                        Console.ForegroundColor = Color.Gray;
                    }
                    break;
            }

            if (config.ShowTestRunnerOutput)
            {
                // show entire output
                Console.ForegroundColor = Color.Gray;
                Console.WriteLine($"\r\n{options.TestRunner} Output [{exitCode}]:");
                Console.ForegroundColor = Color.FromArgb(50, 0, 0);
                Console.WriteLine("============================");
                Console.ForegroundColor = Color.DarkSlateGray;
                Console.WriteLine(output);
                Console.ForegroundColor = Color.Gray;
            }
        }

        private static void ArgsParsingError(IEnumerable<Error> errors)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            foreach (var error in errors)
            {
                switch (error.Tag)
                {
                    case ErrorType.HelpRequestedError:
                        break;
                    case ErrorType.VersionRequestedError:
                        Console.Error.WriteLine($"Copyright \u00A9 {DateTime.Now.Year} Refactor Software Inc.");
                        Console.Error.WriteLine($"https://github.com/replaysMike/NUnit.Commander");
                        break;
                    default:
                        Console.Error.WriteLine($"[{error.Tag}] Error: {error.ToString()}");
                        break;
                }
            }
        }

        private static bool RunLogFriendly(Options options, ApplicationConfiguration configuration, int runNumber, RunContext runContext)
        {
            var isSuccess = false;
            var header = $"NUnit.Commander - Version {Assembly.GetExecutingAssembly().GetName().Version}";
            if (options.Repeat > 1)
                header = header + $", Run #{runNumber}";
            var console = new LogFriendlyConsole(true, header);
            using (var commander = new Commander(configuration, console, runNumber))
            {
                commander.Connect(true, (c) => c.Close());
                commander.WaitForClose();
                isSuccess = commander.RunReports.Count > 0;
                runContext.Runs.Add(commander.ReportContext, commander.RunReports);

                if (runNumber == options.Repeat)
                {
                    // write the final report to the output
                    var reportWriter = new ReportWriter(console, configuration, runContext);
                    reportWriter.WriteFinalReport();
                }
            }
            console.Close();
            console.Dispose();
            return isSuccess;
        }

        private static bool RunFullScreen(Options options, ApplicationConfiguration configuration, int runNumber, RunContext runContext)
        {
            var isSuccess = false;
            var console = new ExtendedConsole();
            var myDataContext = new ConsoleDataContext();
            console.Configure(config =>
            {
                config.SetStaticRow("Header", RowLocation.Top, Color.White, Color.DarkRed);
                config.SetStaticRow("SubHeader", RowLocation.Top, 1, Color.White, Color.FromArgb(30, 30, 30));
                config.SetStaticRow("Footer", RowLocation.Bottom, Color.White, Color.DarkBlue);
                config.SetLogHistoryContainer(RowLocation.Top, 2);
                config.SetDataContext(myDataContext);
                config.SetUpdateInterval(TimeSpan.FromMilliseconds(100));
                config.SetMaxHistoryLines(1000);
                config.SetHelpScreen(new DefaultHelpScreen());
                config.SetQuitHandler((consoleInstance) =>
                {
                    // do something special when quit occurs
                });
            });
            console.OnKeyPress += Console_OnKeyPress;
            console.WriteRow("Header", "NUnit Commander", ColumnLocation.Left, Color.Yellow); // show text on the left
            console.WriteRow("Header", Component.Time, ColumnLocation.Right);
            console.WriteRow("Header", Component.CpuUsage, ColumnLocation.Right);
            if (options.Repeat > 1)
                console.WriteRow("Header", $"Run #{runNumber}", ColumnLocation.Right);
            console.WriteRow("Header", Component.MemoryUsed, ColumnLocation.Right);
            console.WriteRow("SubHeader", "Real-Time Test Monitor", ColumnLocation.Left, Color.FromArgb(60, 60, 60));
            console.Start();

            using (var commander = new Commander(configuration, console, runNumber))
            {
                commander.Connect(true, (c) => c.Close());
                commander.WaitForClose();
                isSuccess = commander.RunReports.Count > 0;
                runContext.Runs.Add(commander.ReportContext, commander.RunReports);

                if (runNumber == options.Repeat)
                {
                    // write the final report to the output
                    var reportWriter = new ReportWriter(console, configuration, runContext);
                    reportWriter.WriteFinalReport();
                }
            }
            console.Flush();
            console.Close();
            console.Dispose();
            return isSuccess;
        }

        private static void Console_OnKeyPress(KeyPressEventArgs e)
        {

        }
    }
}
