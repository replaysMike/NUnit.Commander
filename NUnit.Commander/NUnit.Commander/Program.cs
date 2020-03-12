using AnyConsole;
using CommandLine;
using NUnit.Commander.Analysis;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Console = Colorful.Console;

namespace NUnit.Commander
{
    class Program
    {
        static Commander _commander;

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
            if (options.EventFormatType.HasValue)
                config.EventFormatType = options.EventFormatType.Value;
            if (options.SlowestTestsCount.HasValue)
                config.SlowestTestsCount = options.SlowestTestsCount.Value;
            if (options.ShowTestRunnerOutput.HasValue)
                config.ShowTestRunnerOutput = options.ShowTestRunnerOutput.Value;
            if (options.ColorScheme.HasValue)
                config.ColorScheme = options.ColorScheme.Value;
            if (!string.IsNullOrEmpty(options.LogPath))
                config.LogPath = options.LogPath;

            var colorScheme = new Colors(config.ColorScheme);
            var testRunnerSuccess = true;
            TestRunnerLauncher launcher = null;
            var runNumber = 0;
            var runContext = new RunContext();
            runContext.TestHistoryDatabaseProvider = new TestHistoryDatabaseProvider(config);

            // handle custom operations
            if (options.ListColors)
            {
                colorScheme.PrintColorsToConsole();
                Environment.Exit(0);
            }
            if (options.ClearHistory)
            {
                runContext.TestHistoryDatabaseProvider.DeleteAll();
                runContext.TestHistoryDatabaseProvider.Dispose();
                Environment.Exit(0);
            }

            while (runNumber < options.Repeat)
            {
                runNumber++;
                if (options.TestRunner.HasValue)
                {
                    // launch test runner in another process if asked
                    launcher = new TestRunnerLauncher(options);
                    launcher.OnTestRunnerExit += Launcher_OnTestRunnerExit;
                    testRunnerSuccess = launcher.StartTestRunner();
                }

                var commanderIsSuccess = false;
                if (testRunnerSuccess)
                {
                    // blocking
                    switch (config.DisplayMode)
                    {
                        case DisplayMode.LogFriendly:
                            commanderIsSuccess = RunLogFriendly(options, config, colorScheme, runNumber, runContext);
                            break;
                        case DisplayMode.FullScreen:
                            commanderIsSuccess = RunFullScreen(options, config, colorScheme, runNumber, runContext);
                            break;
                    }

                    if (launcher != null)
                    {
                        //Console.Error.WriteLine($"Exit code: {launcher.ExitCode}");
                        //Console.Error.WriteLine($"OUTPUT: {launcher.ConsoleOutput}");
                        //Console.Error.WriteLine($"ERRORS: {launcher.ConsoleError}");
                        ParseConsoleRunnerOutput(commanderIsSuccess, options, config, colorScheme, launcher);
                        launcher.Dispose();
                    }
                }
            }
        }

        private static void Launcher_OnTestRunnerExit(object sender, EventArgs e)
        {
            var launcher = sender as TestRunnerLauncher;
            if (_commander.IsRunning)
            {
                // unexpected exit
                _commander?.Close();
                System.Threading.Thread.Sleep(100);

                if (!Console.IsOutputRedirected)
                {
                    Console.ResetColor();
                    Console.Clear();
                }
                Console.ForegroundColor = Color.Red;
                Console.Error.WriteLine($"Error: Test runner '{launcher.Options.TestRunner}' closed unexpectedly.");
                Console.Error.WriteLine($"Commander will now exit.");
                Console.ForegroundColor = Color.Gray;
                Environment.Exit(-3);
            }
        }

        private static void ParseConsoleRunnerOutput(bool isSuccess, Options options, ApplicationConfiguration config, Colors colorScheme, TestRunnerLauncher launcher)
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
                            Console.ForegroundColor = colorScheme.DarkError;
                            Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:");
                            Console.ForegroundColor = colorScheme.DarkError;
                            Console.WriteLine("============================");
                            Console.ForegroundColor = colorScheme.Error;
                            Console.WriteLine(errors);
                            Console.ForegroundColor = colorScheme.DarkHighlight;
                        }
                        else
                        {
                            // show entire output
                            Console.ForegroundColor = colorScheme.DarkError;
                            Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:");
                            Console.ForegroundColor = colorScheme.DarkError;
                            Console.WriteLine("============================");
                            Console.ForegroundColor = colorScheme.Error;
                            Console.WriteLine(output);
                            Console.ForegroundColor = colorScheme.DarkHighlight;
                        }
                    }
                    break;
                case TestRunner.DotNetTest:
                    if (config.ShowTestRunnerOutput || (!isSuccess && !string.IsNullOrEmpty(error) && error != Environment.NewLine && !error.Contains("Test Run Failed.")))
                    {
                        Console.ForegroundColor = colorScheme.DarkError;
                        Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:");
                        Console.ForegroundColor = colorScheme.DarkError;
                        Console.WriteLine("============================");
                        Console.ForegroundColor = colorScheme.Error;
                        Console.WriteLine(error);
                        Console.ForegroundColor = colorScheme.DarkHighlight;
                    }
                    else if (config.ShowTestRunnerOutput || (!isSuccess && output.Contains("MSBUILD : error ")))
                    {
                        Console.ForegroundColor = colorScheme.DarkError;
                        Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:");
                        Console.ForegroundColor = colorScheme.DarkError;
                        Console.WriteLine("============================");
                        Console.ForegroundColor = colorScheme.Error;
                        Console.WriteLine(output);
                        Console.ForegroundColor = colorScheme.DarkHighlight;
                    }
                    break;
            }

            if (config.ShowTestRunnerOutput)
            {
                // show entire output
                Console.ForegroundColor = colorScheme.DarkHighlight;
                Console.WriteLine($"\r\n{options.TestRunner} Output [{exitCode}]:");
                Console.ForegroundColor = colorScheme.DarkError;
                Console.WriteLine("============================");
                Console.ForegroundColor = colorScheme.DarkDefault;
                Console.WriteLine(output);
                Console.ForegroundColor = colorScheme.DarkHighlight;
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

        private static bool RunLogFriendly(Options options, ApplicationConfiguration configuration, Colors colorScheme, int runNumber, RunContext runContext)
        {
            var isSuccess = false;
            var header = $"NUnit.Commander - Version {Assembly.GetExecutingAssembly().GetName().Version}";
            if (options.Repeat > 1)
                header = header + $", Run #{runNumber}";
            var console = new LogFriendlyConsole(true, colorScheme, header);
            using (_commander = new Commander(configuration, console, runNumber))
            {
                _commander.Connect(true, (c) => c.Close());
                _commander.WaitForClose();
                isSuccess = _commander.RunReports.Count > 0;
                if (_commander.ReportContext != null)
                    runContext.Runs.Add(_commander.ReportContext, _commander.RunReports);

                // add the run data to the history
                var currentRunHistory = AddCurrentRunToHistory(configuration, runContext);

                if (runNumber == options.Repeat)
                {
                    // write the final report to the output
                    if (configuration.HistoryAnalysisConfiguration.Enabled)
                    {
                        console.WriteLine("Analyzing...");
                        var testHistoryAnalyzer = new TestHistoryAnalyzer(configuration, runContext.TestHistoryDatabaseProvider);
                        runContext.HistoryReport = testHistoryAnalyzer.Analyze(currentRunHistory);
                    }

                    console.WriteLine("Generating report...");
                    var reportWriter = new ReportWriter(console, colorScheme, configuration, runContext);
                    reportWriter.WriteFinalReport();
                }
            }
            console.Close();
            console.Dispose();
            return isSuccess;
        }

        private static bool RunFullScreen(Options options, ApplicationConfiguration configuration, Colors colorScheme, int runNumber, RunContext runContext)
        {
            var isSuccess = false;
            var console = new ExtendedConsole();
            var myDataContext = new ConsoleDataContext();
            console.Configure(config =>
            {
                config.SetStaticRow("Header", RowLocation.Top, colorScheme.Bright, colorScheme.DarkError);
                config.SetStaticRow("SubHeader", RowLocation.Top, 1, colorScheme.Bright, colorScheme.DarkDefault);
                config.SetStaticRow("Footer", RowLocation.Bottom, colorScheme.Bright, colorScheme.DarkHighlight);
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
            console.WriteRow("Header", "NUnit Commander", ColumnLocation.Left, colorScheme.Highlight); // show text on the left
            console.WriteRow("Header", Component.Time, ColumnLocation.Right);
            console.WriteRow("Header", Component.CpuUsage, ColumnLocation.Right);
            if (options.Repeat > 1)
                console.WriteRow("Header", $"Run #{runNumber}", ColumnLocation.Right);
            console.WriteRow("Header", Component.MemoryUsed, ColumnLocation.Right);
            console.WriteRow("SubHeader", "Real-Time Test Monitor", ColumnLocation.Left, colorScheme.DarkDefault);
            console.Start();

            using (_commander = new Commander(configuration, console, runNumber))
            {
                _commander.Connect(true, (c) => c.Close());
                _commander.WaitForClose();
                isSuccess = _commander.RunReports.Count > 0;
                runContext.Runs.Add(_commander.ReportContext, _commander.RunReports);

                // add the run data to the history
                var currentRunHistory = AddCurrentRunToHistory(configuration, runContext);

                if (runNumber == options.Repeat)
                {
                    // write the final report to the output
                    if (configuration.HistoryAnalysisConfiguration.Enabled)
                    {
                        console.WriteLine("Analyzing...");
                        var testHistoryAnalyzer = new TestHistoryAnalyzer(configuration, runContext.TestHistoryDatabaseProvider);
                        runContext.HistoryReport = testHistoryAnalyzer.Analyze(currentRunHistory);
                    }

                    console.WriteLine("Generating report...");
                    var reportWriter = new ReportWriter(console, colorScheme, configuration, runContext);
                    reportWriter.WriteFinalReport();
                }
            }
            console.Flush();
            console.Close();
            console.Dispose();
            return isSuccess;
        }

        private static IEnumerable<TestHistoryEntry> AddCurrentRunToHistory(ApplicationConfiguration configuration, RunContext runContext)
        {
            if (configuration.HistoryAnalysisConfiguration.Enabled)
            {
                var commanderIdMap = runContext.Runs.ToDictionary(key => key.Key.CommanderRunId, value => value.Value.Select(y => y.TestRunId).ToList());
                var allReports = runContext.Runs.SelectMany(x => x.Value);
                var currentRunTestHistoryEntries = allReports
                    .SelectMany(x => x.Report.TestReports
                    .Where(y => y.TestStatus != TestStatus.Skipped)
                    .Select(y => new TestHistoryEntry(commanderIdMap.Where(z => z.Value.Contains(x.TestRunId)).Select(z => z.Key).FirstOrDefault().ToString(), x.TestRunId.ToString(), y)));

                runContext.TestHistoryDatabaseProvider.LoadDatabase();
                runContext.TestHistoryDatabaseProvider.AddTestHistoryRange(currentRunTestHistoryEntries);
                runContext.TestHistoryDatabaseProvider.SaveDatabase();
                return currentRunTestHistoryEntries;
            }
            return null;
        }

        private static void Console_OnKeyPress(KeyPressEventArgs e)
        {

        }
    }
}
