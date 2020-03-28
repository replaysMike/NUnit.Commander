using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Linq;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public class SlowestAssembliesSummary : ReportBase
    {
        public SlowestAssembliesSummary(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorManager colorScheme) : base(configuration, console, runContext, colorScheme) { }

        public override ColorTextBuilder Write(object parameters = null)
        {
            var builder = new ColorTextBuilder();
            WriteRoundBox(builder, $"Top {_configuration.SlowestTestsCount} slowest assemblies");
            var allAssembliesByName = _runContext.Runs
                .SelectMany(x => x.Key.EventEntries)
                .Where(x => x.Event.Event == EventNames.StartAssembly || x.Event.Event == EventNames.EndAssembly)
                .GroupBy(x => x.Event.TestSuite);
            var slowestAssemblies = allAssembliesByName
                .SelectMany(x => x.Select(y => y.Event))
                .Where(x => x.Event == EventNames.EndAssembly)
                .OrderByDescending(x => x.Duration)
                .GroupBy(x => x.TestSuite)
                .Take(_configuration.SlowestTestsCount);
            foreach (var test in slowestAssemblies)
            {
                builder.Append($" {UTF8Constants.Bullet} ");
                builder.Append(DisplayUtil.GetPrettyTestName(test.FirstOrDefault().TestSuite, _colorScheme.DarkDefault, _colorScheme.Default, _colorScheme.DarkDefault));
                builder.AppendLine($" {test.FirstOrDefault().Duration.ToElapsedTime()}", _colorScheme.Duration);
            }
            builder.AppendLine(Environment.NewLine);
            return builder;
        }
    }
}
