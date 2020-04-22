using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Models;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public class HistoricalAnalysisReport : ReportBase
    {
        public HistoricalAnalysisReport(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorScheme colorScheme) : base(configuration, console, runContext, colorScheme) { }

        public override ColorTextBuilder Write(object parameters = null)
        {
            var builder = new ColorTextBuilder();
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.TestAnalysis))
            {
                WriteRoundBox(builder, "Historical Analysis Report");
                // write the analysis report
                if (_runContext.HistoryReport != null)
                    builder.AppendLine(_runContext.HistoryReport.BuildReport());
            }
            return builder;
        }
    }
}
