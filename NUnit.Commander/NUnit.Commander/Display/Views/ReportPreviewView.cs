using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using NUnit.Commander.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Commander.Display.Views
{
    public class ReportPreviewView : IView
    {
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
            context.Console.WriteAt(ColorTextBuilder.Create.AppendLine($"Report Summary - {Constants.KeyboardHelp}", context.ColorScheme.Highlight), 0, 0, DirectOutputMode.Static);
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
                ClearScreen(context);
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

                var yPos = 0;
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

                if (_performFullDraw)
                {
                    // build a report
                    var runContext = new RunContext();
                    var report = context.Commander.CreateReportFromHistory(false);
                    runContext.Runs.Add(context.Commander.GenerateReportContext(false), new List<DataEvent> { report });

                    var reportWriter = new ReportWriter(context.Console, context.ColorScheme, context.Configuration, runContext, false);
                    reportWriter.WriteFinalReport();
                    context.Console.SetCursorPosition(0, 0);
                }
            }
        }
    }
}
