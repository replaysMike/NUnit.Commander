using System.Drawing;

namespace NUnit.Commander.Display
{
    public class MonochromeColorScheme : IColorScheme
    {
        public Color? Background { get; set; } = Color.Black;
        public Color RaisedBackground { get; set; } = Color.FromArgb(16, 32, 16);
        public Color Default { get; set; } = Color.Lime;
        public Color DarkDefault { get; set; } = Color.Lime;
        public Color Bright { get; set; } = Color.Lime;
        public Color Error { get; set; } = Color.Lime;
        public Color DarkError { get; set; } = Color.Lime;
        public Color Success { get; set; } = Color.Lime;
        public Color DarkSuccess { get; set; } = Color.Lime;
        public Color Highlight { get; set; } = Color.Lime;
        public Color DarkHighlight { get; set; } = Color.Lime;
        public Color DarkHighlight2 { get; set; } = Color.Lime;
        public Color DarkHighlight3 { get; set; } = Color.Lime;
        public Color Duration { get; set; } = Color.Lime;
        public Color DarkDuration { get; set; } = Color.Lime;
    }
}
