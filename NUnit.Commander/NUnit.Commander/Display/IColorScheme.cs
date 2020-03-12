using System.Drawing;

namespace NUnit.Commander.Display
{
    /// <summary>
    /// Color scheme
    /// </summary>
    public interface IColorScheme
    {
        Color? Background { get; }
        Color Default { get; }
        Color DarkDefault { get; }
        Color Bright { get; }
        Color Error { get; }
        Color DarkError { get; }
        Color Success { get; }
        Color DarkSuccess { get; }
        Color Highlight { get; }
        Color DarkHighlight { get; }
        Color DarkHighlight2 { get; }
        Color DarkHighlight3 { get; }
        Color Duration { get; }
        Color DarkDuration { get; }
    }
}
