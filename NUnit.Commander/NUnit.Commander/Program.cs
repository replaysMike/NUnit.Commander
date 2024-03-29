﻿using AnyConsole;
using CommandLine;
using NUnit.Commander.Analysis;
using NUnit.Commander.AutoUpdate;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using NUnit.Commander.Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ColorfulConsole = Colorful.Console;
using Console = NUnit.Commander.Display.CommanderConsole;

namespace NUnit.Commander
{
    class Program
    {
        static Commander _commander;
        static TestRunnerLauncher _launcher;
        static bool _applicationQuitRequested;

        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            System.Console.CancelKeyPress += Console_CancelKeyPress;

            try
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
                    .WithParsed(o =>
                    {
                        var isTestPass = Start(o, config);
                        if (isTestPass)
                            exitCode = ExitCode.Success;
                    })
                    .WithNotParsed(errors =>
                    {
                        exitCode = ArgsParsingError(errors);
                    });

                ResetColor();
                return (int)exitCode;
            }
            catch (Exception ex)
            {
                ResetColor();

                Console.Error.WriteLine($"Unhandled exception: {ex.GetBaseException().Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return (int)ExitCode.UnhandledException;
        }

        private static void UpdateConfigOverrides(Options options, ApplicationConfiguration config)
        {
            // override any configuration options via commandline
            if (options.EnableLog.HasValue)
                config.EnableLog = options.EnableLog.Value;
            if (options.EnableTestLog.HasValue)
                config.EnableTestLog = options.EnableTestLog.Value;
            if (options.EnableReportLog.HasValue)
                config.EnableReportLog = options.EnableReportLog.Value;
            if (options.DontPrettify.HasValue)
                config.DontPrettify = options.DontPrettify.Value;
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
            if (options.DotNetConnectTimeoutSeconds.HasValue)
                config.DotNetConnectTimeoutSeconds = options.DotNetConnectTimeoutSeconds.Value;
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
            if (!string.IsNullOrEmpty(options.HistoryPath))
                config.HistoryPath = options.HistoryPath;
        }

        private static bool Start(Options options, ApplicationConfiguration config)
        {
            var isTestPass = false;
            var hasAnotherRunner = true;

            UpdateConfigOverrides(options, config);

            var colorScheme = new ColorScheme(config.ColorScheme);
            Console.SetColorScheme(colorScheme);
            var runNumber = 0;
            var runContext = new RunContext
            {
                TestHistoryDatabaseProvider = new TestHistoryDatabaseProvider(config)
            };

            HandleCustomOperations(options, colorScheme, runContext);
            DisplayLogo(colorScheme);
            AutoUpdate(options, colorScheme);
            DisplayInitializationScreen(options, config, colorScheme, runContext);

            while (!_applicationQuitRequested && runNumber < options.Repeat)
            {
                runNumber++;

                if (options.TestRunner.HasValue)
                {
                    _launcher = new TestRunnerLauncher(options);
                    _launcher.OnScanStarted = () =>
                    {
                        Console.WriteLine($"Scanning test assemblies...", colorScheme.Default);
                    };
                    _launcher.OnScanCompleted = () =>
                    {
                        Console.WriteLine($"Done scanning test assemblies!", colorScheme.Default);
                    };
                    _launcher.OnTestRunnerExit += Launcher_OnTestRunnerExit;
                    // launch test runner in another process if asked
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.TimeOfDay}] Test Runner started!");
                    var canLaunch = _launcher.QueueTestRunners();
                    if (!canLaunch)
                    {
                        Console.WriteLine($"Error launching the test runner!", colorScheme.Default);
                    }
                }

                var runnerTask = () =>
                {
                    // start the process immediately
                    return _launcher?.NextProcess() ?? false;
                };

                while (!_applicationQuitRequested && hasAnotherRunner) /*(_launcher?.NextProcess() ?? false)*/
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.TimeOfDay}] Commander Launching {config.DisplayMode}");
                    // blocking
                    switch (config.DisplayMode)
                    {
                        case DisplayMode.LogFriendly:
                            (isTestPass, hasAnotherRunner) = RunLogFriendly(options, config, colorScheme, runNumber, runContext, runnerTask);
                            break;
                        case DisplayMode.FullScreen:
                            (isTestPass, hasAnotherRunner) = RunFullScreen(options, config, colorScheme, runNumber, runContext, runnerTask);
                            break;
                        default:
                            Console.WriteLine($"Unknown DisplayMode '{config.DisplayMode}'");
                            break;
                    }
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

            ResetColor();

            return isTestPass;

            static void HandleCustomOperations(Options options, ColorScheme colorScheme, RunContext runContext)
            {
                // handle custom operations
                if (options.ListColors)
                {
                    colorScheme.PrintColorMap();
                    // ResetColor();
                    Environment.Exit((int)ExitCode.Success);
                }
                if (options.ClearHistory)
                {
                    runContext.TestHistoryDatabaseProvider.DeleteAll();
                    runContext.TestHistoryDatabaseProvider.Dispose();
                    ResetColor();
                    Environment.Exit((int)ExitCode.Success);
                }
            }

            static void DisplayLogo(ColorScheme colorScheme)
            {
                // display logo
                if (!Console.IsOutputRedirected)
                {
                    Console.Clear();
                    var fontBytes = ResourceLoader.Load("big.flf");
                    var font = Colorful.FigletFont.Load(fontBytes);
                    ColorfulConsole.WriteAscii(Constants.ApplicationName, font, colorScheme.Highlight);
                    ColorfulConsole.WriteLine($"Version {Assembly.GetExecutingAssembly().GetName().Version}", colorScheme.DarkHighlight);
                    ColorfulConsole.WriteLine(new string(UTF8Constants.BoxHorizontal, Console.WindowWidth - 5), colorScheme.DarkHighlight);
                }
            }

            static void AutoUpdate(Options options, ColorScheme colorScheme)
            {
                // check for application updates
                try
                {
                    if (options.AutoUpdate && AutoUpdater.CheckForUpdate())
                        AutoUpdater.PerformUpdate(options, colorScheme);
                }
                catch (Exception ex)
                {
                    ColorfulConsole.WriteLine($"Warning: Auto-update failed. Error: {ex.GetBaseException().Message}", colorScheme.DarkError);
                }
            }

            static void DisplayInitializationScreen(Options options, ApplicationConfiguration config, ColorScheme colorScheme, RunContext runContext)
            {
                // initialize the performance counters before launching the test runner
                // this is because it can be slow, we don't want to delay connecting to the test runner
                try
                {
                    if (!Console.IsOutputRedirected)
                    {
                        var currentFont = ConsoleUtil.GetCurrentFont().FontName;
                        Console.Write($"Console font: ");
                        Console.WriteLine(currentFont, colorScheme.DarkDefault);
                        var unicodeTestHeader = "Unicode test:";
                        Console.Write(unicodeTestHeader);
                        Console.WriteLine("\u2022 ╭╮╰╯═══\u2801\u2802\u2804\u2840\u28FF", colorScheme.DarkDefault);
                        var conEmuDetected = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConEmuPID"));
                        var parentProcess = ParentProcessUtilities.GetParentProcess();
                        var powershellDetected = parentProcess?.ProcessName.Equals("powershell", StringComparison.InvariantCultureIgnoreCase) == true;
                        var dotChar = ConsoleUtil.GetCharAt(unicodeTestHeader.Length, Console.CursorTop); // dot ok
                        var brailleChar = ConsoleUtil.GetCharAt(unicodeTestHeader.Length + 9, Console.CursorTop); // braille ok
                        var dotCharOk = false;
                        if (OperatingSystem.IsWindows())
                            dotCharOk = ConsoleUtil.CheckIfCharInFont(dotChar, new Font(currentFont, 10));

                        var brailleCharOk = false;
                        if (OperatingSystem.IsWindows())
                            brailleCharOk = ConsoleUtil.CheckIfCharInFont(brailleChar, new Font(currentFont, 10));
                        // Console.WriteLine($"Dot: {dotCharOk}, Braille: {brailleCharOk}");
                        Console.Write($"Console Detection: ");
                        if (conEmuDetected)
                        {
                            Console.WriteLine("ConEmu", colorScheme.DarkDefault);
                            config.DisplayConfiguration.IsConEmuDetected = true;
                            config.DisplayConfiguration.SupportsExtendedUnicode = true;
                        }
                        else if (powershellDetected)
                        {
                            Console.WriteLine("Powershell", colorScheme.DarkDefault);
                            config.DisplayConfiguration.IsPowershellDetected = true;
                        }
                        else
                        {
                            Console.WriteLine("Command Prompt", colorScheme.DarkDefault);
                            config.DisplayConfiguration.IsCommandPromptDetected = true;
                        }
                    }
                    Console.Write($"Test runner arguments: ");
                    Console.WriteLine(options.TestRunnerArguments.MaxLength(360), colorScheme.DarkDefault);

                    Console.WriteLine($"Initializing performance counters...", colorScheme.Default);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
#pragma warning disable CA1416 // Validate platform compatibility
                        runContext.PerformanceCounters.CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                        runContext.PerformanceCounters.DiskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
#pragma warning restore CA1416 // Validate platform compatibility
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing performance counters... {ex.Message}", colorScheme.Error);
                    // unable to use performance counters.
                    // possibly need to run C:\Windows\SysWOW64> lodctr /r
                }
            }
        }

        private static void Launcher_OnTestRunnerExit(object sender, EventArgs e)
        {
            var launcher = sender as TestRunnerLauncher;
            // sleep a little bit and give commander a chance to tell is if it's running.
            // sometimes the final report event is delayed a very little bit.
            System.Diagnostics.Debug.WriteLine("Test runner exited! Waiting a bit...");
            _commander?.WaitForClose(3000);
            System.Diagnostics.Debug.WriteLine("Wait for commander to close complete.");

            if (_launcher.HasErrors)
            {
                // unexpected exit
                _commander?.Close();

                Console.ForegroundColor = Color.Red;
                Console.Error.WriteLine($"Error: Test runner '{launcher.Options.TestRunner}' closed unexpectedly with exit code ({launcher.ExitCode}).");
                Console.Error.WriteLine($"Commander will now exit.");
                Console.ForegroundColor = Color.Gray;
                if (!string.IsNullOrEmpty(launcher.ConsoleError?.Trim()))
                {
                    Console.ForegroundColor = Color.White;
                    Console.Error.WriteLine($"{launcher.TestRunnerName} error output: {launcher.ConsoleError}");
                }
                if (!string.IsNullOrEmpty(launcher.ConsoleOutput?.Trim()))
                {
                    Console.ForegroundColor = Color.White;
                    Console.Error.WriteLine($"{launcher.TestRunnerName} output: {launcher.ConsoleOutput}");
                }

                ResetColor();
                Environment.Exit((int)ExitCode.TestRunnerExited);
            }
        }

        private static void ParseConsoleRunnerOutput(bool isSuccess, Options options, ApplicationConfiguration config, ColorScheme colorScheme)
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
                case TestRunner.Auto:
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

        private static (bool isPass, bool hasAnotherRunner) RunLogFriendly(Options options, ApplicationConfiguration configuration, ColorScheme colorScheme, int runNumber, RunContext runContext, Func<bool> testRunnerTask)
        {
            var isSuccess = false;
            var hasAnotherRunner = false;
            var console = new LogFriendlyConsole(false, colorScheme);
            console.OnKeyPress += Console_OnKeyPress;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.TimeOfDay}] Commander created");
                using (_commander = new Commander(configuration, colorScheme, console, runNumber, runContext))
                {
                    if (!Console.IsOutputRedirected)
                        Console.Clear();
                    // execute the test runner
                    hasAnotherRunner = testRunnerTask.Invoke();
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
                            var testHistoryAnalyzer = new TestHistoryAnalyzer(configuration, colorScheme, runContext.TestHistoryDatabaseProvider);
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
                        var reportWriter = new ReportWriter(console, colorScheme, configuration, runContext, true);
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
            return (isSuccess, hasAnotherRunner);
        }

        private static (bool isPass, bool hasAnotherRunner) RunFullScreen(Options options, ApplicationConfiguration configuration, ColorScheme colorScheme, int runNumber, RunContext runContext, Func<bool> testRunnerTask)
        {
            var isSuccess = false;
            var hasAnotherRunner = false;
            var console = new ExtendedConsole();
            var myDataContext = new ConsoleDataContext();
            ConfigureConsole(options, colorScheme, runNumber, console, myDataContext);

            try
            {
                using (_commander = new Commander(configuration, colorScheme, console, runNumber, runContext))
                {
                    // execute the test runner
                    hasAnotherRunner = testRunnerTask.Invoke();
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
                            var testHistoryAnalyzer = new TestHistoryAnalyzer(configuration, colorScheme, runContext.TestHistoryDatabaseProvider);
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
                        var reportWriter = new ReportWriter(console, colorScheme, configuration, runContext, true);
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
            return (isSuccess, hasAnotherRunner);

            static void ConfigureConsole(Options options, ColorScheme colorScheme, int runNumber, ExtendedConsole console, ConsoleDataContext myDataContext)
            {
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
            }
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

        private static void ResetColor()
        {
            if (!Console.IsOutputRedirected)
            {
                Console.ColorScheme?.ResetColor();
                Console.CursorVisible = true;
                Console.ForegroundColor = Color.Gray;
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            ResetColor();
            // Quit application and show report immediately
            try
            {
                _applicationQuitRequested = true;
                if (_launcher != null)
                    _launcher.OnTestRunnerExit -= Launcher_OnTestRunnerExit;
                var report = _commander?.CreateReportFromHistory();
                _commander?.RunReports.Add(report);
                _commander?.Close();
                _launcher.Kill();
            }
            catch (Exception)
            {
                // something went wrong, exit application
            }
            finally
            {
                Console.WriteLine("Application force quitting (CTRL-C pressed)...");
            }
        }

        private static void Console_OnKeyPress(KeyPressEventArgs e)
        {
            switch (e.Key)
            {
                case ConsoleKey.Q:
                    // Quit application and show report immediately
                    _applicationQuitRequested = true;
                    if (_launcher != null)
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
