using AnyConsole;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public interface IReportWriter
    {
        ColorTextBuilder Write(object parameters = null);
    }
}
