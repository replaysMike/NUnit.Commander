using AnyConsole;
using CommandLine;
using NUnit.Commander.Configuration;
using NUnit.Commander.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
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

            var parser = new Parser(c => {
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
            if (options.DisplayMode.HasValue)
                config.DisplayMode = options.DisplayMode.Value;
            if (options.ConnectTimeoutSeconds.HasValue)
                config.ConnectTimeoutSeconds = options.ConnectTimeoutSeconds.Value;
            if (options.GenerateReportType.HasValue)
                config.GenerateReportType = options.GenerateReportType.Value;
            if (options.SlowestTestsCount.HasValue)
                config.SlowestTestsCount = options.SlowestTestsCount.Value;
            if (!string.IsNullOrEmpty(options.LogPath))
                config.LogPath = options.LogPath;

            var testRunnerSuccess = true;
            TestRunnerLauncher launcher = null;
            if (options.TestRunner.HasValue)
            {
                // launch test runner in another process if asked
                launcher = new TestRunnerLauncher(options);
                testRunnerSuccess = launcher.StartTestRunner();
            }

            if (testRunnerSuccess)
            {
                // blocking
                switch (config.DisplayMode)
                {
                    case DisplayMode.FullScreen:
                        RunFullScreen(config);
                        break;
                    case DisplayMode.LogFriendly:
                        RunLogFriendly(config);
                        break;
                }

                if (launcher != null)
                {
                    //Console.Error.WriteLine($"Exit code: {launcher.ExitCode}");
                    //Console.Error.WriteLine($"OUTPUT: {launcher.ConsoleOutput}");
                    //Console.Error.WriteLine($"ERRORS: {launcher.ConsoleError}");
                    ParseConsoleRunnerOutput(options.TestRunner.Value, launcher);
                    launcher.Dispose();
                }
            }
        }

        private static void ParseConsoleRunnerOutput(TestRunner testRunner, TestRunnerLauncher launcher)
        {
            launcher.WaitForExit();
            var output = launcher.ConsoleOutput;
            var error = launcher.ConsoleError;
            var exitCode = launcher.ExitCode;
            switch (testRunner)
            {
                case TestRunner.NUnitConsole:
                    if (exitCode < 0)
                    {
                        var startErrorsIndex = output.IndexOf("Errors, Failures and Warnings");
                        if (startErrorsIndex >= 0)
                        {
                            var endErrorsIndex = output.IndexOf("Test Run Summary", startErrorsIndex);
                            var errors = output.Substring(startErrorsIndex, endErrorsIndex - startErrorsIndex);
                            Console.ForegroundColor = Color.DarkRed;
                            Console.WriteLine($"\r\nNUnit-Console Error Output [{exitCode}]:");
                            Console.ForegroundColor = Color.FromArgb(50, 0, 0);
                            Console.WriteLine("============================");
                            Console.ForegroundColor = Color.Red;
                            Console.WriteLine(errors);
                            Console.ForegroundColor = Color.Gray;
                        }
                        else
                        {
                            Console.ForegroundColor = Color.DarkRed;
                            Console.WriteLine($"\r\nNUnit-Console Error Output [{exitCode}]:");
                            Console.ForegroundColor = Color.FromArgb(50, 0, 0);
                            Console.WriteLine("============================");
                            Console.ForegroundColor = Color.Red;
                            Console.WriteLine(output);
                            Console.ForegroundColor = Color.Gray;
                        }
                    }
                    break;
                case TestRunner.DotNetTest:
                    if (!string.IsNullOrEmpty(error) && error != Environment.NewLine && !error.Contains("Test Run Failed."))
                    {
                        Console.ForegroundColor = Color.DarkRed;
                        Console.WriteLine($"\r\nDotNetTest Error Output [{exitCode}]:");
                        Console.ForegroundColor = Color.FromArgb(50, 0, 0);
                        Console.WriteLine("============================");
                        Console.ForegroundColor = Color.Red;
                        Console.WriteLine(error);
                        Console.ForegroundColor = Color.Gray;
                    }
                    break;
            }
        }

        private static void ArgsParsingError(IEnumerable<Error> errors)
        {
            foreach(var error in errors)
            {
                if (error.Tag != ErrorType.HelpRequestedError)
                    Console.Error.WriteLine($"Error: [{error.Tag}] {error.ToString()}");
            }
        }

        private static void RunLogFriendly(ApplicationConfiguration configuration)
        {
            var console = new LogFriendlyConsole();
            using (var commander = new Commander(configuration, console))
            {
                commander.ConnectIpcServer(true, (c) => c.Close());
                commander.WaitForClose();
                console.Close();
                console.Dispose();
            }
        }

        private static void RunFullScreen(ApplicationConfiguration configuration)
        {
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
                config.SetQuitHandler((consoleInstance) => {
                    // do something special when quit occurs
                });
            });
            console.OnKeyPress += Console_OnKeyPress; ;
            console.WriteRow("Header", "NUnit Commander", ColumnLocation.Left, Color.Yellow); // show text on the left
            console.WriteRow("Header", Component.Time, ColumnLocation.Right); // show the time on the right
            console.WriteRow("SubHeader", "Real-Time Test Monitor", ColumnLocation.Left, Color.FromArgb(60, 60, 60));
            console.Start();

            using (var commander = new Commander(configuration, console))
            {
                commander.ConnectIpcServer(true, (c) => c.Close());
                commander.WaitForClose();
            }
            console.Flush();
            console.Close();
            console.Dispose();
        }

        private static void Console_OnKeyPress(KeyPressEventArgs e)
        {
            
        }
    }
}
