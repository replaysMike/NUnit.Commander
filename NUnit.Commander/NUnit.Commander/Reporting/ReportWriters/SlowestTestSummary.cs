using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Linq;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public class SlowestTestSummary : ReportBase
    {
        public SlowestTestSummary(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorScheme colorScheme) : base(configuration, console, runContext, colorScheme) { }

        public override ColorTextBuilder Write(object parameters = null)
        {
            var builder = new ColorTextBuilder();
            WriteRoundBox(builder, $"Top {_configuration.SlowestTestsCount} slowest tests");
            var allTestsByName = _runContext.Runs
                .SelectMany(x => x.Key.EventEntries)
                .GroupBy(x => x.Event.TestName);
            var slowestTests = allTestsByName
                .SelectMany(x => x.Select(y => y.Event))
                .Where(x => x.Event == EventNames.EndTest)
                .OrderByDescending(x => x.Duration)
                .GroupBy(x => x.TestName)
                .Take(_configuration.SlowestTestsCount);
            foreach (var test in slowestTests)
            {
                builder.Append($" {UTF8Constants.Bullet} ");
                builder.Append(DisplayUtil.GetPrettyTestName(test.FirstOrDefault().FullName, _colorScheme.DarkDefault, _colorScheme.Default, _colorScheme.DarkDefault));
                builder.AppendLine($" {test.FirstOrDefault().Duration.ToElapsedTime()}", _colorScheme.Duration);
            }
            builder.AppendLine(Environment.NewLine);
            return builder;
        }
    }
}
