using AnyConsole;
using CommandLine;
using NUnit.Commander.Analysis;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Console = NUnit.Commander.Display.CommanderConsole;
using ColorfulConsole = Colorful.Console;

namespace NUnit.Commander
{
    class Program
    {
        static Commander _commander;
        static TestRunnerLauncher _launcher;

        static int Main(string[] args)
        {
            var exitCode = ExitCode.TestsFailed;
            var configProvider = new ConfigurationProvider();
            var configuration = configProvider.LoadConfiguration();
            var config = configProvider.Get<ApplicationConfiguration>(configuration);

            var parser = new Parser(c =>
            {
                c.CaseSensitive = false;
                c.HelpWriter = Console.Error;
            });
            parser.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    var isTestPass = Start(o, config);
                    if (isTestPass)
                        exitCode = ExitCode.Success;
                })
                .WithNotParsed<Options>(errors =>
                {
                    exitCode = ArgsParsingError(errors);
                });

            return (int)exitCode;
        }

        private static bool Start(Options options, ApplicationConfiguration config)
        {
            var isTestPass = false;

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
            if (options.ExitOnFirstTestFailure.HasValue)
                config.ExitOnFirstTestFailure = options.ExitOnFirstTestFailure.Value;
            if (!string.IsNullOrEmpty(options.LogPath))
                config.LogPath = options.LogPath;

            var colorScheme = new ColorManager(config.ColorScheme);
            Console.SetColorManager(colorScheme);
            var testRunnerSuccess = true;
            var runNumber = 0;
            var runContext = new RunContext();
            runContext.TestHistoryDatabaseProvider = new TestHistoryDatabaseProvider(config);

            // handle custom operations
            if (options.ListColors)
            {
                colorScheme.PrintColorsToConsole();
                if (!Console.IsOutputRedirected)
                    Console.CursorVisible = true;
                Environment.Exit(0);
            }
            if (options.ClearHistory)
            {
                runContext.TestHistoryDatabaseProvider.DeleteAll();
                runContext.TestHistoryDatabaseProvider.Dispose();
                if (!Console.IsOutputRedirected)
                    Console.CursorVisible = true;
                Environment.Exit(0);
            }

            // display logo
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
                var fontBytes = ResourceLoader.Load("big.flf");
                var font = Colorful.FigletFont.Load(fontBytes);
                ColorfulConsole.WriteAscii("NUnit Commander", font, Color.Yellow);
                ColorfulConsole.WriteLine($"Version {Assembly.GetExecutingAssembly().GetName().Version}", Color.Yellow);
            }

            // initialize the performance counters before launching the test runner
            // this is because it can be slow, we don't want to delay connecting to the test runner
            try
            {
                Console.WriteLine($"Initializing performance counters...", colorScheme.Default);
                runContext.PerformanceCounters.CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                runContext.PerformanceCounters.DiskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing performance counters... {ex.Message}", colorScheme.Default);
                // unable to use performance counters.
                // possibly need to run C:\Windows\SysWOW64> lodctr /r
            }

            while (runNumber < options.Repeat)
            {
                runNumber++;
                if (options.TestRunner.HasValue)
                {
                    // launch test runner in another process if asked
                    _launcher = new TestRunnerLauncher(options);
                    _launcher.OnTestRunnerExit += Launcher_OnTestRunnerExit;
                    testRunnerSuccess = _launcher.StartTestRunner();
                }

                if (testRunnerSuccess)
                {
                    // blocking
                    switch (config.DisplayMode)
                    {
                        case DisplayMode.LogFriendly:
                            isTestPass = RunLogFriendly(options, config, colorScheme, runNumber, runContext);
                            break;
                        case DisplayMode.FullScreen:
                            isTestPass = RunFullScreen(options, config, colorScheme, runNumber, runContext);
                            break;
                        default:
                            Console.WriteLine($"Unknown DisplayMode '{config.DisplayMode}'");
                            break;
                    }

                    if (_launcher != null)
                    {
                        // kill the test runner if it's still running at this point
                        _launcher.Kill();
                        //Console.Error.WriteLine($"Exit code: {launcher.ExitCode}");
                        //Console.Error.WriteLine($"OUTPUT: {launcher.ConsoleOutput}");
                        //Console.Error.WriteLine($"ERRORS: {launcher.ConsoleError}");
                        ParseConsoleRunnerOutput(isTestPass, options, config, colorScheme);
                        _launcher.Dispose();
                        runContext?.PerformanceCounters?.CpuCounter?.Dispose();
                        runContext?.PerformanceCounters?.DiskCounter?.Dispose();
                    }
                }
            }

            if (!Console.IsOutputRedirected)
                Console.ResetColor();

            return isTestPass;
        }

        private static void Launcher_OnTestRunnerExit(object sender, EventArgs e)
        {
            var launcher = sender as TestRunnerLauncher;
            // sleep a little bit and give commander a chance to tell is if it's running.
            // sometimes the final report event is delayed a very little bit.
            _commander.WaitForClose(3000);

            if (_commander.IsRunning)
            {
                // unexpected exit
                _commander?.Close();

                if (!Console.IsOutputRedirected)
                {
                    Console.ResetColor();
                    Console.Clear();
                    Console.CursorVisible = true;
                }
                Console.ForegroundColor = Color.Red;
                Console.Error.WriteLine($"Error: Test runner '{launcher.Options.TestRunner}' closed unexpectedly.");
                Console.Error.WriteLine($"Commander will now exit.");
                Console.ForegroundColor = Color.Gray;
                Environment.Exit((int)ExitCode.TestRunnerExited);
            }
        }

        private static void ParseConsoleRunnerOutput(bool isSuccess, Options options, ApplicationConfiguration config, ColorManager colorScheme)
        {
            _launcher.WaitForExit();
            var output = _launcher.ConsoleOutput;
            var error = _launcher.ConsoleError;
            var exitCode = _launcher.ExitCode;
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
                            Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:", colorScheme.DarkError);
                            Console.WriteLine(Constants.SimpleSeparator, colorScheme.DarkError);
                            Console.WriteLine(errors, colorScheme.Error);
                            Console.ForegroundColor = colorScheme.DarkHighlight;
                        }
                        else
                        {
                            // show entire output
                            Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:", colorScheme.DarkError);
                            Console.WriteLine(Constants.SimpleSeparator, colorScheme.DarkError);
                            Console.WriteLine(output, colorScheme.DarkError);
                            Console.ForegroundColor = colorScheme.DarkHighlight;
                        }
                    }
                    break;
                case TestRunner.DotNetTest:
                    if (config.ShowTestRunnerOutput || (!isSuccess && !string.IsNullOrEmpty(error) && error != Environment.NewLine && !error.Contains("Test Run Failed.")))
                    {
                        Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:", colorScheme.DarkError);
                        Console.WriteLine(Constants.SimpleSeparator, colorScheme.DarkError);
                        Console.WriteLine(error, colorScheme.Error);
                        Console.ForegroundColor = colorScheme.DarkHighlight;
                    }
                    else if (config.ShowTestRunnerOutput || (!isSuccess && output.Contains("MSBUILD : error ")))
                    {
                        Console.WriteLine($"\r\n{options.TestRunner} Error Output [{exitCode}]:", colorScheme.DarkError);
                        Console.WriteLine(Constants.SimpleSeparator, colorScheme.DarkError);
                        Console.WriteLine(output, colorScheme.Error);
                        Console.ForegroundColor = colorScheme.DarkHighlight;
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown TestRunner '{options.TestRunner}'", colorScheme.Error);
                    break;
            }

            if (config.ShowTestRunnerOutput)
            {
                // show entire output
                Console.WriteLine($"\r\n{options.TestRunner} Output [{exitCode}]:", colorScheme.DarkError);
                Console.WriteLine(Constants.SimpleSeparator, colorScheme.DarkError);
                Console.WriteLine(output, colorScheme.DarkDefault);
                Console.ForegroundColor = colorScheme.DarkHighlight;
            }
        }

        private static ExitCode ArgsParsingError(IEnumerable<Error> errors)
        {
            var exitCode = ExitCode.InvalidArguments;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            foreach (var error in errors)
            {
                switch (error.Tag)
                {
                    case ErrorType.HelpRequestedError:
                        exitCode = ExitCode.HelpRequested;
                        break;
                    case ErrorType.VersionRequestedError:
                        Console.Error.WriteLine(Constants.Copyright);
                        Console.Error.WriteLine(Constants.WebsiteUrl);
                        exitCode = ExitCode.VersionRequested;
                        break;
                    default:
                        Console.Error.WriteLine($"[{error.Tag}] Error: {error.ToString()}");
                        exitCode = ExitCode.InvalidArguments;
                        break;
                }
            }
            return exitCode;
        }

        private static bool RunLogFriendly(Options options, ApplicationConfiguration configuration, ColorManager colorScheme, int runNumber, RunContext runContext)
        {
            var isSuccess = false;
            var console = new LogFriendlyConsole(false, colorScheme);
            console.OnKeyPress += Console_OnKeyPress;

            try
            {
                using (_commander = new Commander(configuration, console, runNumber, runContext))
                {
                    _commander.Connect(true, (c) =>
                    {
                        if (!Console.IsOutputRedirected)
                            Console.Clear();
                    }, (c) => c.Close());
                    _commander.WaitForClose();
                    runContext.Runs.Add(_commander.GenerateReportContext(), _commander.RunReports);

                    // add the run data to the history
                    var currentRunHistory = AddCurrentRunToHistory(configuration, runContext);

                    if (runNumber >= options.Repeat)
                    {
                        // write the final report to the output
                        if (configuration.HistoryAnalysisConfiguration.Enabled)
                        {
                            Console.WriteLine("Analyzing...", colorScheme.Default);
                            var testHistoryAnalyzer = new TestHistoryAnalyzer(configuration, runContext.TestHistoryDatabaseProvider);
                            runContext.HistoryReport = testHistoryAnalyzer.Analyze(currentRunHistory);
                        }

                        var runCount = runContext.Runs.Count;
                        var testCount = runContext.Runs.SelectMany(x => x.Value).Sum(x => x.TestCount);
                        if (testCount == 0)
                        {
                            var report = _commander?.CreateReportFromHistory();
                            _commander.RunReports.Add(report);
                        }
                        Console.WriteLine($"Generating report for {runCount} run(s), {testCount} tests...", colorScheme.Default);
                        var reportWriter = new ReportWriter(console, colorScheme, configuration, runContext);
                        console.Clear();
                        if (reportWriter.WriteFinalReport() == TestStatus.Pass)
                            isSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Commander encountered an unhandled exception: {ex.GetBaseException().Message} Stack Trace: {ex.StackTrace}", colorScheme.Error);
            }
            finally
            {
                console.Close();
                console.Dispose();
            }
            return isSuccess;
        }

        private static bool RunFullScreen(Options options, ApplicationConfiguration configuration, ColorManager colorScheme, int runNumber, RunContext runContext)
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
            console.WriteRow("Header", Constants.ApplicationName, ColumnLocation.Left, colorScheme.Highlight); // show text on the left
            console.WriteRow("Header", Component.Time, ColumnLocation.Right);
            console.WriteRow("Header", Component.CpuUsage, ColumnLocation.Right);
            if (options.Repeat > 1)
                console.WriteRow("Header", $"Run #{runNumber}", ColumnLocation.Right);
            console.WriteRow("Header", Component.MemoryUsed, ColumnLocation.Right);
            console.WriteRow("SubHeader", "Real-Time Test Monitor", ColumnLocation.Left, colorScheme.DarkDefault);
            console.Start();

            try
            {
                using (_commander = new Commander(configuration, console, runNumber, runContext))
                {
                    _commander.Connect(true, (c) => { }, (c) => c.Close());
                    _commander.WaitForClose();
                    runContext.Runs.Add(_commander.GenerateReportContext(), _commander.RunReports);

                    // add the run data to the history
                    var currentRunHistory = AddCurrentRunToHistory(configuration, runContext);

                    if (runNumber >= options.Repeat)
                    {
                        // write the final report to the output
                        if (configuration.HistoryAnalysisConfiguration.Enabled)
                        {
                            Console.WriteLine("Analyzing...", colorScheme.Default);
                            var testHistoryAnalyzer = new TestHistoryAnalyzer(configuration, runContext.TestHistoryDatabaseProvider);
                            runContext.HistoryReport = testHistoryAnalyzer.Analyze(currentRunHistory);
                        }

                        var runCount = runContext.Runs.Count;
                        var testCount = runContext.Runs.SelectMany(x => x.Value).Sum(x => x.TestCount);
                        if (testCount == 0)
                        {
                            var report = _commander?.CreateReportFromHistory();
                            _commander.RunReports.Add(report);
                        }
                        Console.WriteLine($"Generating report for {runCount} run(s), {testCount} tests...", colorScheme.Default);
                        var reportWriter = new ReportWriter(console, colorScheme, configuration, runContext);
                        console.Clear();
                        if (reportWriter.WriteFinalReport() == TestStatus.Pass)
                            isSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Commander encountered an unhandled exception: {ex.GetBaseException().Message} Stack Trace: {ex.StackTrace}", colorScheme.Error);
            }
            finally
            {
                console.Flush();
                console.Close();
                console.Dispose();
            }
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
            switch (e.Key)
            {
                case ConsoleKey.Q:
                    // Quit application and show report immediately
                    _launcher.OnTestRunnerExit -= Launcher_OnTestRunnerExit;
                    var report = _commander?.CreateReportFromHistory();
                    _commander?.RunReports.Add(report);
                    _commander?.Close();
                    _launcher.Kill();
                    break;
                case ConsoleKey.P:
                    _commander?.TogglePauseDisplay();
                    break;
                case ConsoleKey.Tab:
                    // switch view
                    _commander?.UnpauseDisplay();
                    if (e.KeyState.HasFlag(ControlKeyState.SHIFT_PRESSED))
                        _commander?.PreviousView();
                    else
                        _commander?.NextView();
                    break;
                default:
                    break;
            }
        }
    }
}
