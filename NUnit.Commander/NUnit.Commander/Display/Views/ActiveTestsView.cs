using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NUnit.Commander.Display.Views
{
    public class ActiveTestsView : IView
    {
        private const string _bulletChar = "\u2022";
        private long _startTicks = 0;
        private string _version;
        private int _bottomPadding = 1;
        private int _previousWindowHeight = 0;

        public ActiveTestsView()
        {
            _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public void Deactivate()
        {
            _startTicks = 0;
        }

        private void WriteHeader(ViewContext context)
        {
            var header = $"{Constants.ApplicationName} - Version {_version}, Run #{context.Commander.RunNumber}";
            if (!context.Console.IsOutputRedirected)
                header += $" - {Constants.KeyboardHelp}";
            context.Console.WriteAt(ColorTextBuilder.Create.AppendLine(header, context.ColorScheme.Highlight), 0, 0, DirectOutputMode.Static);
        }

        private void ClearScreen(ViewContext context)
        {
            context.Console.Clear();
            WriteHeader(context);
        }

        public void Draw(ViewContext context, long ticks)
        {
            if (_startTicks == 0)
            {
                _startTicks = ticks;
                ClearScreen(context);
            }

            var yPos = context.BeginY;
            var drawChecksum = 0;
            var performDrawByTime = true;
            var performDrawByDataChange = true;
            var activeTestsCountChanged = context.ActiveTests.Count != context.LastNumberOfTestsRunning;
            var windowWidth = 160;
            var windowHeight = 40;
            if (!context.Console.IsOutputRedirected)
            {
                windowWidth = Console.WindowWidth;
                windowHeight = Console.WindowHeight;
                if (windowHeight != _previousWindowHeight)
                    ClearScreen(context);
                _previousWindowHeight = windowHeight;
            }

            if (context.Console.IsOutputRedirected)
            {
                // if any tests have changed state based on checksum, allow a redraw
                drawChecksum = ComputeActiveTestChecksum(context);
                performDrawByTime = DateTime.Now.Subtract(context.LastDrawTime).TotalMilliseconds > context.DrawIntervalMilliseconds;
                performDrawByDataChange = drawChecksum != context.LastDrawChecksum;
            }
            if ((performDrawByTime || performDrawByDataChange) && context.ActiveTests.Any())
            {
                if (!context.Console.IsOutputRedirected)
                    WriteHeader(context);

                // figure out how many tests we can fit on screen
                var maxActiveTestsToDisplay = context.Configuration.MaxActiveTestsToDisplay;
                if (!context.Console.IsOutputRedirected && maxActiveTestsToDisplay == 0)
                    maxActiveTestsToDisplay = Console.WindowHeight - yPos - 2 - context.Configuration.MaxFailedTestsToDisplay - 4;

                context.LastDrawChecksum = drawChecksum;
                if (context.Console.IsOutputRedirected)
                    context.Console.WriteLine();
                else if (activeTestsCountChanged)
                {
                    // number of tests changed
                    var nextNumberOfTestsDrawn = Math.Min(context.ActiveTests.Count, maxActiveTestsToDisplay);
                    if (nextNumberOfTestsDrawn < context.LastNumberOfTestsDrawn)
                    {
                        // clear the static display if we are displaying less tests than the previous draw
                        if (!context.Console.IsOutputRedirected)
                        {
                            var startY = yPos + nextNumberOfTestsDrawn + 3;
                            var endY = startY + (context.LastNumberOfTestsDrawn - nextNumberOfTestsDrawn);
                            context.Console.ClearAtRange(0, startY, 0, endY);
                        }
                    }
                    context.LastNumberOfTestsRunning = context.ActiveTests.Count;
                }
                var testNumber = 0;
                var totalActive = context.ActiveTests.Count(x => !x.IsQueuedForRemoval);
                var totalPasses = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Pass);
                var totalFails = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail);
                var totalIgnored = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Skipped);
                var totalTestsProcessed = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest);

                context.LastNumberOfLinesDrawn = 0;
                // write the summary of all test state
                context.Console.WriteAt(ColorTextBuilder.Create
                        .Append("Tests state: ", context.ColorScheme.Bright)
                        .Append($"Active=", context.ColorScheme.Default)
                        .Append($"{totalActive} ", context.ColorScheme.Highlight)
                        .Append($"Pass=", context.ColorScheme.Default)
                        .AppendIf(totalPasses > 0, $"{totalPasses} ", context.ColorScheme.Success)
                        .AppendIf(totalPasses == 0, $"{totalPasses} ", context.ColorScheme.Default)
                        .Append($"Fail=", context.ColorScheme.Default)
                        .AppendIf(totalFails > 0, $"{totalFails} ", context.ColorScheme.DarkError)
                        .AppendIf(totalFails == 0, $"{totalFails} ", context.ColorScheme.Default)
                        .AppendIf(!context.Console.IsOutputRedirected, $"Ignored=", context.ColorScheme.Default)
                        .AppendIf(!context.Console.IsOutputRedirected, $"{totalIgnored} ", context.ColorScheme.DarkDefault)
                        .Append($"Total=", context.ColorScheme.Default)
                        .Append($"{totalTestsProcessed} ", context.ColorScheme.DarkDefault)
                        .AppendIf(context.TotalTestsQueued > 0, $"of ", context.ColorScheme.Default)
                        .AppendIf(context.TotalTestsQueued > 0, $"{context.TotalTestsQueued} ", context.ColorScheme.DarkDefault)
                        .AppendIf(context.TotalTestsQueued > 0, $"[", context.ColorScheme.Bright)
                        .AppendIf(context.TotalTestsQueued > 0, $"{((totalTestsProcessed / (double)context.TotalTestsQueued) * 100.0):F0}%", context.ColorScheme.DarkDuration)
                        .AppendIf(context.TotalTestsQueued > 0, $"]", context.ColorScheme.Bright)
                        .Append(context.Client.IsWaitingForConnection ? $"[waiting]" : "", context.ColorScheme.DarkDuration)
                        .AppendIf(!context.Console.IsOutputRedirected, (length) => new string(' ', Math.Max(0, windowWidth - length))),
                        0,
                        yPos,
                        DirectOutputMode.Static);
                context.LastNumberOfLinesDrawn++;
                if (!context.Console.IsOutputRedirected)
                {
                    context.Console.WriteAt(ColorTextBuilder.Create
                        .Append("Runtime: ", context.ColorScheme.Bright)
                        .Append($"{DateTime.Now.Subtract(context.StartTime).ToTotalElapsedTime()} ", context.ColorScheme.Duration)
                        .Append((length) => new string(' ', Math.Max(0, windowWidth - length))),
                        0,
                        yPos + 1,
                        DirectOutputMode.Static);
                    context.LastNumberOfLinesDrawn++;
                }
                if (!context.Console.IsOutputRedirected && !string.IsNullOrEmpty(context.CurrentFrameworkVersion))
                {
                    context.Console.WriteAt(ColorTextBuilder.Create
                        .Append($"{context.CurrentFrameworkVersion}", context.ColorScheme.DarkDuration)
                        .AppendIf(!context.Console.IsOutputRedirected, (length) => new string(' ', Math.Max(0, windowWidth - length))),
                        0, yPos + context.LastNumberOfLinesDrawn, DirectOutputMode.Static);
                    context.LastNumberOfLinesDrawn++;
                }

                // **************************
                // Draw Active Tests
                // **************************
                IEnumerable<EventEntry> activeTestsToDisplay;
                if (context.Console.IsOutputRedirected)
                {
                    // for log file output only show running tests
                    activeTestsToDisplay = context.ActiveTests
                        .Where(x => x.Event.TestStatus == TestStatus.Running && !x.IsQueuedForRemoval)
                        .OrderByDescending(x => x.Elapsed);
                }
                else
                {
                    activeTestsToDisplay = context.ActiveTests
                        .OrderBy(x => x.Event.TestStatus)
                        .ThenByDescending(x => x.Elapsed)
                        .Take(maxActiveTestsToDisplay);
                }

                foreach (var test in activeTestsToDisplay)
                {
                    testNumber++;
                    var lifetime = DateTime.Now.Subtract(test.Event.StartTime);
                    if (test.Event.EndTime != DateTime.MinValue)
                        lifetime = test.Event.Duration;
                    var testColor = context.ColorScheme.Highlight;
                    var testStatus = "INVD";
                    switch (test.Event.TestStatus)
                    {
                        case TestStatus.Pass:
                            testStatus = "PASS";
                            testColor = context.ColorScheme.Success;
                            break;
                        case TestStatus.Fail:
                            testStatus = "FAIL";
                            testColor = context.ColorScheme.Error;
                            break;
                        case TestStatus.Skipped:
                            testStatus = "SKIP";
                            testColor = context.ColorScheme.DarkDefault;
                            break;
                        case TestStatus.Running:
                        default:
                            testStatus = "RUN ";
                            testColor = context.ColorScheme.Highlight;
                            break;
                    }

                    var prettyTestName = DisplayUtil.GetPrettyTestName(test.Event.TestName, context.ColorScheme.DarkDefault, context.ColorScheme.Default, context.ColorScheme.DarkDefault, context.MaxTestCaseArgumentLength);
                    // print out this test name and duration
                    context.Console.WriteAt(ColorTextBuilder.Create
                        // test number
                        .Append($"{testNumber}: ", context.ColorScheme.DarkDefault)
                        // spaced in columns
                        .AppendIf(testNumber < 10 && !context.Console.IsOutputRedirected, $" ")
                        // test status if not logging to file
                        .AppendIf(!context.Console.IsOutputRedirected, "[", context.ColorScheme.DarkDefault)
                        .AppendIf(!context.Console.IsOutputRedirected, testStatus, testColor)
                        .AppendIf(!context.Console.IsOutputRedirected, "] ", context.ColorScheme.DarkDefault)
                        // test name
                        .Append(prettyTestName)
                        // test duration
                        .Append($" {lifetime.ToTotalElapsedTime()}", context.ColorScheme.Duration)
                        // clear out the rest of the line
                        .AppendIf((length) => !context.Console.IsOutputRedirected && length < windowWidth, (length) => new string(' ', Math.Max(0, windowWidth - length)))
                        .Truncate(windowWidth),
                        0,
                        yPos + context.LastNumberOfLinesDrawn,
                        DirectOutputMode.Static);
                    context.LastNumberOfLinesDrawn++;
                }
                context.LastNumberOfTestsDrawn = testNumber;

                // **************************
                // Draw Test Failures
                // **************************
                if (!context.Console.IsOutputRedirected && context.Configuration.MaxFailedTestsToDisplay > 0)
                {
                    var failedTests = context.EventLog
                        .Where(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail)
                        .GroupBy(x => x.Event.TestName)
                        .Select(x => x.FirstOrDefault())
                        .OrderByDescending(x => x.DateAdded)
                        .Take(context.Configuration.MaxFailedTestsToDisplay);
                    if (failedTests.Any())
                    {
                        var totalFailedTests = failedTests.Count();
                        context.Console.WriteAt(ColorTextBuilder.Create.AppendLine($"{context.Configuration.MaxFailedTestsToDisplay} Most Recent Failed Tests", context.ColorScheme.Error), 0, windowHeight - totalFailedTests - _bottomPadding - 1, DirectOutputMode.Static);
                        var failedTestNumber = 0;
                        foreach (var test in failedTests)
                        {
                            failedTestNumber++;
                            var duration = DateTime.Now.Subtract(test.Event.StartTime);
                            if (test.Event.EndTime != DateTime.MinValue)
                                duration = test.Event.Duration;
                            var prettyTestName = DisplayUtil.GetPrettyTestName(test.Event.FullName, context.ColorScheme.DarkDefault, context.ColorScheme.Default, context.ColorScheme.DarkDefault, context.MaxTestCaseArgumentLength);
                            // print out this test name and duration
                            context.Console.WriteAt(ColorTextBuilder.Create
                                .Append($" {_bulletChar} ")
                                .Append(prettyTestName)
                                .Append($" {duration.ToTotalElapsedTime()}", context.ColorScheme.Duration)
                                .Append(" [", context.ColorScheme.Bright)
                                .Append("FAILED", context.ColorScheme.Error)
                                .Append("] ", context.ColorScheme.Bright)
                                .Append($"{test.DateAdded.ToString(Constants.TimeFormat)}", context.ColorScheme.DarkDuration)
                                // clear out the rest of the line
                                .AppendIf((length) => !context.Console.IsOutputRedirected && length < windowWidth, (length) => new string(' ', Math.Max(0, windowWidth - length)))
                                .Truncate(windowWidth),
                                0,
                                windowHeight - totalFailedTests + failedTestNumber - _bottomPadding - 1,
                                DirectOutputMode.Static);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compute a checksum to see if any tests have changed state
        /// </summary>
        /// <returns></returns>
        private int ComputeActiveTestChecksum(ViewContext viewContext)
        {
            var hc = viewContext.ActiveTests.Count;
            for (var i = 0; i < viewContext.ActiveTests.Count; ++i)
                hc = unchecked(hc * 314159 + viewContext.ActiveTests[i].GetHashCode());
            return hc;
        }
    }
}
