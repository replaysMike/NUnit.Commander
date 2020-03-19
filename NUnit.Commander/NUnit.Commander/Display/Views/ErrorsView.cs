using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Linq;

namespace NUnit.Commander.Display.Views
{
    public class ErrorsView : IView
    {
        private const string _bulletChar = "\u2022";
        private const char _lineChar = '`';
        private const int DefaultDrawFps = 2;
        private const int DefaultTickWait = (int)(1000.0 / 66) * DefaultDrawFps;
        private long _startTicks = 0;
        private bool _performFullDraw = false;
        private int _previousWindowHeight = 0;

        public void Deactivate()
        {
            _startTicks = 0;
        }

        private void WriteHeader(ViewContext context)
        {
            context.Console.WriteAt(ColorTextBuilder.Create.AppendLine($"Errors Summary - {Constants.KeyboardHelp}", context.ColorScheme.Highlight), 0, 0, DirectOutputMode.Static);
        }

        private void ClearScreen(ViewContext context)
        {
            context.Console.Clear();
        }

        public void Draw(ViewContext context, long ticks)
        {
            if (_startTicks == 0)
            {
                _startTicks = ticks;
                context.Console.Clear();
            }

            if (!context.Console.IsOutputRedirected)
            {
                // if the user has scrolled the page don't perform any drawing
                if (Console.WindowTop > 0)
                    return;

                _performFullDraw = (_startTicks - ticks) % DefaultTickWait == 0;
                var windowWidth = Console.WindowWidth;
                var windowHeight = Console.WindowHeight;
                if (windowHeight != _previousWindowHeight)
                    _performFullDraw = true;
                _previousWindowHeight = windowHeight;

                if (_performFullDraw)
                    ClearScreen(context);

                var lineSeparator = new string(_lineChar, Console.WindowWidth / 2);
                var yPos = 0;
                // figure out how many tests we can fit on screen
                var maxActiveTestsToDisplay = Console.WindowHeight - yPos - 2;

                var totalActive = context.ActiveTests.Count(x => !x.IsQueuedForRemoval);
                var totalPasses = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Pass);
                var totalFails = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail);
                var totalIgnored = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Skipped);
                var totalTestsProcessed = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest);

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
                        yPos + 1,
                        DirectOutputMode.Static);

                var failedTests = context.EventLog
                    .Where(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail)
                    .GroupBy(x => x.Event.TestName)
                    .Select(x => x.FirstOrDefault())
                    .OrderByDescending(x => x.DateAdded)
                    .Take(context.Configuration.MaxFailedTestsToDisplay);
                context.Console.WriteAt(ColorTextBuilder.Create.AppendLine($"Failed Tests Report", context.ColorScheme.Error), 0, yPos + 2, DirectOutputMode.Static);
                if (!failedTests.Any())
                {
                    context.Console.WriteAt(ColorTextBuilder.Create.AppendLine($"There are no failed tests.", context.ColorScheme.Default), 0, yPos + 4, DirectOutputMode.Static);
                }
                else
                {
                    // don't display this as often as its expensive to write
                    if (_performFullDraw)
                    {
                        context.Console.SetCursorPosition(0, yPos + 5);
                        foreach (var test in failedTests)
                        {
                            var testOutput = new ColorTextBuilder();
                            var errorTime = test.DateAdded;
                            var duration = DateTime.Now.Subtract(test.Event.StartTime);
                            if (test.Event.EndTime != DateTime.MinValue)
                                duration = test.Event.Duration;
                            var prettyTestName = DisplayUtil.GetPrettyTestName(test.Event.FullName, context.ColorScheme.DarkDefault, context.ColorScheme.Default, context.ColorScheme.DarkDefault, context.MaxTestCaseArgumentLength);
                            // print out this test name and duration
                            testOutput.Append(ColorTextBuilder.Create
                                .Append($" {_bulletChar} ")
                                .Append(prettyTestName)
                                .Append($" {duration.ToTotalElapsedTime()}", context.ColorScheme.Duration)
                                .Append(" [", context.ColorScheme.Bright)
                                .Append("FAILED", context.ColorScheme.Error)
                                .Append("]", context.ColorScheme.Bright)
                                // clear out the rest of the line
                                .AppendIf((length) => !context.Console.IsOutputRedirected && length < windowWidth, (length) => new string(' ', Math.Max(0, windowWidth - length)))
                                .Truncate(windowWidth));
                            testOutput.Append(ColorTextBuilder.Create.Append("  Failed at: ", context.ColorScheme.Default).AppendLine($"{errorTime.ToString("hh:mm:ss.fff tt")}", context.ColorScheme.DarkDuration));

                            // print out errors
                            if (!string.IsNullOrEmpty(test.Event.ErrorMessage))
                            {
                                testOutput.AppendLine($"  Error Output: ", context.ColorScheme.Bright);
                                testOutput.AppendLine(lineSeparator, context.ColorScheme.DarkDefault);
                                testOutput.AppendLine($"{test.Event.ErrorMessage}", context.ColorScheme.DarkError);
                                testOutput.AppendLine(lineSeparator, context.ColorScheme.DarkDefault);
                            }
                            if (!string.IsNullOrEmpty(test.Event.StackTrace))
                            {
                                testOutput.AppendLine($"  Stack Trace:", context.ColorScheme.Bright);
                                testOutput.AppendLine(lineSeparator, context.ColorScheme.DarkDefault);
                                testOutput.AppendLine($"{test.Event.StackTrace}", context.ColorScheme.DarkError);
                                testOutput.AppendLine(lineSeparator, context.ColorScheme.DarkDefault);
                            }
                            if (!string.IsNullOrEmpty(test.Event.TestOutput))
                            {
                                testOutput.AppendLine($"  Test Output: ", context.ColorScheme.Bright);
                                testOutput.AppendLine(lineSeparator, context.ColorScheme.DarkDefault);
                                testOutput.AppendLine($"{test.Event.TestOutput}", context.ColorScheme.Default);
                                testOutput.AppendLine(lineSeparator, context.ColorScheme.DarkDefault);
                            }
                            testOutput.AppendLine();
                            testOutput.AppendLine();

                            context.Console.WriteLine(testOutput);
                        }

                        context.Console.SetCursorPosition(0, 0);
                    }
                }
            }
        }
    }
}
