using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Models;
using System.Drawing;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public abstract class ReportBase : IReportWriter
    {
        internal const int DefaultBorderWidth = 50;

        internal ApplicationConfiguration _configuration;
        internal IExtendedConsole _console;
        internal RunContext _runContext;
        internal ColorScheme _colorScheme;

        private ReportBase() { }
        internal ReportBase(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorScheme colorScheme)
        {
            _configuration = configuration;
            _console = console;
            _runContext = runContext;
            _colorScheme = colorScheme;
        }

        internal void WriteSquareBox(ColorTextBuilder builder, string str, int leftPadding = 0, Color? color = null)
        {
            builder.AppendLine($"{UTF8Constants.BoxTopLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.BoxHorizontal)}{UTF8Constants.BoxTopRight}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.BoxVertical}  {str}  {UTF8Constants.BoxVertical}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.BoxBottomLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.BoxHorizontal)}{UTF8Constants.BoxBottomRight}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
        }

        internal void WriteRoundBox(ColorTextBuilder builder, string str, int leftPadding = 0, Color? color = null)
        {
            builder.AppendLine($"{UTF8Constants.RoundBoxTopLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.RoundBoxHorizontal)}{UTF8Constants.RoundBoxTopRight}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.RoundBoxVertical}  {str}  {UTF8Constants.RoundBoxVertical}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.RoundBoxBottomLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.RoundBoxHorizontal)}{UTF8Constants.RoundBoxBottomRight}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
        }

        public abstract ColorTextBuilder Write(object parameters = null);
    }
}
