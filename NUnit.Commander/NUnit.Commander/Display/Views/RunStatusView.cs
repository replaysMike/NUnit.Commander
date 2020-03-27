using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Linq;

namespace NUnit.Commander.Display.Views
{
    public class RunStatusView : IView
    {
        private const int DefaultDrawFps = 2;
        private const int DefaultTickWait = (int)(1000.0 / 66) * DefaultDrawFps;
        private const string TimeFormat = "h:mm:ss";
        private long _startTicks = 0;
        private bool _performFullDraw = false;
        private int _previousWindowHeight = 0;

        public void Deactivate()
        {
            _startTicks = 0;
        }

        private void WriteHeader(ViewContext context)
        {
            context.Console.WriteAt(ColorTextBuilder.Create.AppendLine($"Run Status - {Constants.KeyboardHelp}", context.ColorScheme.Highlight), 0, 0, DirectOutputMode.Static);
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

                Console.CursorVisible = false;
                _performFullDraw = (_startTicks - ticks) % DefaultTickWait == 0;
                var windowWidth = Console.WindowWidth;
                var windowHeight = Console.WindowHeight;
                if (windowHeight != _previousWindowHeight)
                    _performFullDraw = true;
                _previousWindowHeight = windowHeight;

                if (_performFullDraw)
                    ClearScreen(context);

                var lineSeparator = DisplayUtil.Pad(Console.WindowWidth - 1, UTF8Constants.HorizontalLine);
                var yPos = 0;
                // figure out how many tests we can fit on screen
                var maxActiveTestsToDisplay = Console.WindowHeight - yPos - 2;

                var totalActiveTests = context.ActiveTests.Count(x => !x.IsQueuedForRemoval);
                var totalActiveTestFixtures = context.ActiveTestFixtures.Count(x => !x.IsQueuedForRemoval);
                var totalActiveAssemblies = context.ActiveAssemblies.Count(x => !x.IsQueuedForRemoval);
                if (totalActiveTests > 0 && totalActiveTestFixtures == 0)
                {
                    // fix for older versions of nUnit that don't send the start test fixture event
                    // it will still be incorrect, as it will treat parents of tests with multiple cases as a testfixture
                    var parentIds = context.ActiveTests
                        .Where(x => !x.IsQueuedForRemoval && !string.IsNullOrEmpty(x.Event.ParentId))
                        .Select(x => x.Event.ParentId)
                        .Distinct();
                    totalActiveTestFixtures = context.ActiveTestSuites.Count(x => !x.IsQueuedForRemoval && parentIds.Contains(x.Event.Id));
                }
                var totalPasses = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Pass);
                var totalFails = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Fail);
                var totalIgnored = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest && x.Event.TestStatus == TestStatus.Skipped);
                var totalTestsProcessed = context.EventLog.Count(x => x.Event.Event == EventNames.EndTest);
                WriteHeader(context);

                // write the summary of all test state
                context.Console.WriteAt(ColorTextBuilder.Create
                        .Append("Tests state: ", context.ColorScheme.Bright)
                        .Append($"Tests=", context.ColorScheme.Default)
                        .Append($"{totalActiveTests} ", context.ColorScheme.Highlight)
                        .Append($"Fixtures=", context.ColorScheme.Default)
                        .Append($"{totalActiveTestFixtures} ", context.ColorScheme.Highlight)
                        .Append($"Assemblies=", context.ColorScheme.Default)
                        .Append($"{totalActiveAssemblies} ", context.ColorScheme.Highlight)
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
                        .AppendIf(context.TotalTestsQueued > 0, $"{UTF8Constants.LeftBracket}", context.ColorScheme.Bright)
                        .AppendIf(context.TotalTestsQueued > 0, $"{((totalTestsProcessed / (double)context.TotalTestsQueued) * 100.0):F0}%", context.ColorScheme.DarkDuration)
                        .AppendIf(context.TotalTestsQueued > 0, $"{UTF8Constants.RightBracket}", context.ColorScheme.Bright)
                        .Append(context.Client.IsWaitingForConnection ? $"{UTF8Constants.LeftBracket}waiting{UTF8Constants.RightBracket}" : "", context.ColorScheme.DarkDuration)
                        .AppendIf(!context.Console.IsOutputRedirected, (length) => DisplayUtil.Pad(windowWidth - length)),
                        0,
                        yPos + 1,
                        DirectOutputMode.Static);

                // don't display this as often as its expensive to write
                if (_performFullDraw)
                {
                    context.Console.SetCursorPosition(0, 2);
                    var allCompletedAssemblies = context.EventLog
                        .Where(x => x.Event.Event == EventNames.EndAssembly)
                        .GroupBy(x => x.Event.TestSuite)
                        .Select(x => x.FirstOrDefault())
                        .OrderByDescending(x => x.Event.Duration);
                    var allPendingAssemblies = context.EventLog
                        .Where(x => x.Event.Event == EventNames.StartAssembly && !allCompletedAssemblies.Select(y => y.Event.TestSuite).Contains(x.Event.TestSuite))
                        .GroupBy(x => x.Event.TestSuite)
                        .Select(x => x.FirstOrDefault())
                        .OrderByDescending(x => x.DateAdded);
                    var completedAssemblies = allCompletedAssemblies.Take(20);
                    var pendingAssemblies = allPendingAssemblies.Take(20);
                    var completedAssembliesBuilder = new ColorTextBuilder();
                    completedAssembliesBuilder.Append($"Completed Assemblies ", context.ColorScheme.Bright)
                        .Append("[").Append($"{allCompletedAssemblies.Count()}", context.ColorScheme.Duration).Append("]")
                        .AppendLine();

                    if (completedAssemblies.Any())
                    {
                        foreach (var assembly in completedAssemblies)
                        {
                            var entryOutput = new ColorTextBuilder();
                            var completionTime = assembly.DateAdded;
                            var duration = DateTime.Now.Subtract(assembly.Event.StartTime);
                            if (assembly.Event.EndTime != DateTime.MinValue)
                                duration = assembly.Event.Duration;
                            var prettyTestName = DisplayUtil.GetPrettyTestName(assembly.Event.TestSuite, context.ColorScheme.DarkDefault, context.ColorScheme.Default, context.ColorScheme.DarkDefault, context.MaxTestCaseArgumentLength);
                            // print out this test name and duration
                            entryOutput
                                .Append($"[{completionTime.ToString(TimeFormat)}] ", context.ColorScheme.DarkDuration)
                                .Append(prettyTestName)
                                .Append($" {duration.ToTotalElapsedTime()}", context.ColorScheme.Duration);

                            completedAssembliesBuilder.AppendLine(entryOutput);
                        }
                    }

                    var activeAssembliesBuilder = new ColorTextBuilder();
                    activeAssembliesBuilder.Append($"Running Assemblies", context.ColorScheme.Bright)
                        .Append("[").Append($"{allPendingAssemblies.Count()}", context.ColorScheme.Duration).Append("]")
                        .AppendLine();
                    if (pendingAssemblies.Any())
                    {
                        foreach (var assembly in pendingAssemblies)
                        {
                            var entryOutput = new ColorTextBuilder();
                            var completionTime = assembly.DateAdded;
                            var duration = DateTime.Now.Subtract(assembly.Event.StartTime);
                            if (assembly.Event.EndTime != DateTime.MinValue)
                                duration = assembly.Event.Duration;
                            var prettyTestName = DisplayUtil.GetPrettyTestName(assembly.Event.TestSuite, context.ColorScheme.DarkDefault, context.ColorScheme.Default, context.ColorScheme.DarkDefault, context.MaxTestCaseArgumentLength);
                            // print out this test name and duration
                            entryOutput
                                .Append(prettyTestName)
                                .Append($" {duration.ToTotalElapsedTime()}", context.ColorScheme.Duration);

                            activeAssembliesBuilder.AppendLine(entryOutput);
                        }
                    }

                    // write builders side-by-side
                    var columnSpacing = 2;
                    var columnWidth = (context.Console.WindowWidth / 2) - (columnSpacing * 2);
                    var output = completedAssembliesBuilder.Interlace(activeAssembliesBuilder, columnSpacing, columnWidth);
                    context.Console.WriteLine(output);

                    // output complete
                    context.Console.SetCursorPosition(0, 0);
                }
            }
        }
    }
}
