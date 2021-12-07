using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public class Overview : ReportBase
    {
        public Overview(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorScheme colorScheme) : base(configuration, console, runContext, colorScheme) { }

        public override ColorTextBuilder Write(object parameters = null)
        {
            const string longTimeFormat = "hh:mm:ss.fff tt";
            var allReports = (IEnumerable<DataEvent>)parameters;
            var builder = new ColorTextBuilder();
            var headerLine = new string(UTF8Constants.BoxHorizontal, DefaultBorderWidth);
            var startTime = _runContext.Runs.Select(x => x.Key.StartTime).OrderBy(x => x).FirstOrDefault();
            var endTime = _runContext.Runs.Select(x => x.Key.EndTime).OrderByDescending(x => x).FirstOrDefault();
            if (endTime == DateTime.MinValue)
                endTime = DateTime.Now;
            var totalDuration = _runContext.Elapsed;
            builder.AppendLine();
            builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxTopLeft}{headerLine}{headerLine}", _colorScheme.Highlight).Append($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight).Append($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight2).AppendLine($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight3)
                .AppendLine($"{UTF8Constants.BoxVertical}  NUnit.Commander Test Report", _colorScheme.Highlight));
            var testRunIds = allReports.GroupBy(x => x.TestRunId).Select(x => x.Key);
            if (testRunIds?.Any() == true)
                builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"  Test Run Id(s): {string.Join(", ", testRunIds)}"));
            var frameworks = _runContext.Runs.SelectMany(x => x.Key.Frameworks).Distinct();
            if (frameworks.Any() == true)
                builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"  Framework(s): {string.Join(", ", frameworks)}"));
            var frameworkRuntimes = _runContext.Runs.SelectMany(x => x.Key.FrameworkRuntimes).Distinct();
            if (frameworkRuntimes.Any() == true)
                builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"  Framework Runtime(s): {string.Join(", ", frameworkRuntimes)}"));
            builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).Append($"  Job Start: {startTime.ToString(longTimeFormat)}"));
            builder.Append($"  Job End: {endTime.ToString(longTimeFormat)}");
            builder.AppendLine($"  Total Job Duration: {endTime.Subtract(startTime)}");
            builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"  Settings:"));
            builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"    Test Runtime={totalDuration}"));
            if (_console.IsOutputRedirected)
                builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"    LogMode=Enabled"));
            else
                builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"    LogMode=Disabled"));
            builder.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxBottomLeft}{headerLine}{headerLine}", _colorScheme.Highlight).Append($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight).Append($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight2).AppendLine($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight3));
            return builder;
        }
    }
}
