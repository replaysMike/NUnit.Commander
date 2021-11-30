using System;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Text;

namespace NUnit.Commander.Display
{
    /// <summary>
    /// A replacement for the System Console
    /// </summary>
    public static class CommanderConsole
    {
        private static Lazy<ColorScheme> _map = new Lazy<ColorScheme>(() => new ColorScheme(Configuration.ColorSchemes.Default));

        /// <summary>
        /// Get the current color scheme
        /// </summary>
        internal static ColorScheme ColorScheme => _map.Value;

        internal static void SetColorScheme(ColorScheme colorManager)
        {
            _map = new Lazy<ColorScheme>(colorManager);
        }

        public static string GetCurrentFont() => ConsoleUtil.GetCurrentFont().FontName;

        public static void Write(string text) => Console.Write(text);
        public static void Write(string text, Color color)
        {
            if (_map != null)
            {
                var foreColor = Console.ForegroundColor;
                Console.ForegroundColor = _map.Value.GetMappedConsoleColor(color) ?? ConsoleColor.Gray;
                Console.Write(text);
                Console.ForegroundColor = foreColor;
            }
            else
            {
                Console.Write(text);
            }
        }
        public static void WriteLine(string text) => Console.WriteLine(text);
        public static void WriteLine(string text, Color color)
        {
            if (_map != null)
            {
                var foreColor = Console.ForegroundColor;
                Console.ForegroundColor = _map.Value.GetMappedConsoleColor(color) ?? ConsoleColor.Gray;
                Console.WriteLine(text);
                Console.ForegroundColor = foreColor;
            }
            else
            {
                Console.WriteLine(text);
            }
        }
        public static void WriteLine() => Console.WriteLine();
        public static void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);
        public static void Clear() => Console.Clear();
        public static void Clear(Color color)
        {
            if (_map != null)
            {
                var consoleColor = _map.Value.GetMappedConsoleColor(color);
                if (consoleColor != null)
                    Console.BackgroundColor = consoleColor.Value;
                else
                    _map.Value.ResetBackgroundColor();
            }
            Console.Clear();
        }
        public static void ResetColor() => Console.ResetColor();
        public static string ReadLine() => Console.ReadLine();

        public static bool CursorVisible
        {
            [SupportedOSPlatformGuard("windows")]
            get
            {
                if (OperatingSystem.IsWindows())
                    return Console.CursorVisible;
                return false;
            }
            set => Console.CursorVisible = value;
        }

        public static int WindowWidth
        {
            get => Console.WindowWidth;
            [SupportedOSPlatformGuard("windows")]
            set
            {
                if (OperatingSystem.IsWindows())
                    Console.WindowWidth = value;
            }
        }

        public static int WindowHeight
        {
            get => Console.WindowHeight;
            [SupportedOSPlatformGuard("windows")]
            set
            {
                if (OperatingSystem.IsWindows())
                    Console.WindowHeight = value;
            }
        }

        public static int WindowTop
        {
            get => Console.WindowTop;
            [SupportedOSPlatformGuard("windows")]
            set
            {
                if (OperatingSystem.IsWindows())
                    Console.WindowTop = value;
            }
        }

        public static int WindowLeft
        {
            get => Console.WindowLeft;
            [SupportedOSPlatformGuard("windows")]
            set
            {
                if (OperatingSystem.IsWindows())
                    Console.WindowLeft = value;
            }
        }

        public static int CursorTop
        {
            get => Console.CursorTop;
            set => Console.CursorTop = value;
        }

        public static int CursorLeft
        {
            get => Console.CursorLeft;
            set => Console.CursorLeft = value;
        }

        public static int CursorSize
        {
            get => Console.CursorSize;
            [SupportedOSPlatformGuard("windows")]
            set
            {
                if (OperatingSystem.IsWindows())
                    Console.CursorSize = value;
            }
        }

        public static Encoding OutputEncoding
        {
            get => Console.OutputEncoding;
            set => Console.OutputEncoding = value;
        }

        public static Color BackgroundColor
        {
            get { return _map.Value.GetMappedColor(Console.BackgroundColor); }
            set
            {
                var consoleColor = _map.Value.GetMappedConsoleColor(value);
                if (consoleColor != null)
                    Console.BackgroundColor = consoleColor.Value;
                else
                    _map.Value.ResetBackgroundColor();
            }
        }

        public static Color ForegroundColor
        {
            get { return _map.Value.GetMappedColor(Console.ForegroundColor); }
            set
            {
                var consoleColor = _map.Value.GetMappedConsoleColor(value);
                if (consoleColor != null)
                    Console.ForegroundColor = consoleColor.Value;
                else
                    _map.Value.ResetForegroundColor();
            }
        }

        public static bool IsOutputRedirected => Console.IsOutputRedirected;

        public static bool IsErrorRedirected => Console.IsErrorRedirected;

        public static TextWriter Out => Console.Out;

        public static TextWriter Error => Console.Error;
    }
}
