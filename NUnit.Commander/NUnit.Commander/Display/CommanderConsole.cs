using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace NUnit.Commander.Display
{
    /// <summary>
    /// A replacement for the System Console
    /// </summary>
    public static class CommanderConsole
    {
        private static ColorScheme _map;

        /// <summary>
        /// Get the current color scheme
        /// </summary>
        internal static ColorScheme ColorScheme => _map = new ColorScheme(Configuration.ColorSchemes.Default);

        internal static void SetColorScheme(ColorScheme colorManager)
        {
            _map = colorManager;
        }

        public static string GetCurrentFont() => ConsoleUtil.GetCurrentFont().FontName;

        public static void Write(string text) => Console.Write(text);
        public static void Write(string text, Color color)
        {
            if (_map != null)
            {
                var foreColor = Console.ForegroundColor;
                Console.ForegroundColor = _map.GetMappedConsoleColor(color);
                Console.Write(text, _map.GetMappedConsoleColor(color));
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
                Console.ForegroundColor = _map.GetMappedConsoleColor(color);
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
                Console.BackgroundColor = _map.GetMappedConsoleColor(color);
            Console.Clear();
        }
        public static void ResetColor() => Console.ResetColor();
        public static string ReadLine() => Console.ReadLine();

        public static bool CursorVisible
        {
            get { return Console.CursorVisible; }
            set { Console.CursorVisible = value; }
        }
        public static int WindowWidth
        {
            get { return Console.WindowWidth; }
            set { Console.WindowWidth = value; }
        }

        public static int WindowHeight
        {
            get { return Console.WindowHeight; }
            set { Console.WindowHeight = value; }
        }

        public static int WindowTop
        {
            get { return Console.WindowTop; }
            set { Console.WindowTop = value; }
        }

        public static int WindowLeft
        {
            get { return Console.WindowLeft; }
            set { Console.WindowLeft = value; }
        }

        public static int CursorTop
        {
            get { return Console.CursorTop; }
            set { Console.CursorTop = value; }
        }

        public static int CursorLeft
        {
            get { return Console.CursorLeft; }
            set { Console.CursorLeft = value; }
        }

        public static int CursorSize
        {
            get { return Console.CursorSize; }
            set { Console.CursorSize = value; }
        }

        public static Encoding OutputEncoding
        {
            get { return Console.OutputEncoding; }
            set { Console.OutputEncoding = value; }
        }

        public static Color BackgroundColor
        {
            get { return _map?.GetMappedColor(Console.BackgroundColor) ?? Color.Black; }
            set { Console.BackgroundColor = _map?.GetMappedConsoleColor(value) ?? ConsoleColor.Black; }
        }

        public static Color ForegroundColor
        {
            get { return _map?.GetMappedColor(Console.ForegroundColor) ?? Color.Gray; }
            set { Console.ForegroundColor = _map?.GetMappedConsoleColor(value) ?? ConsoleColor.Gray; }
        }

        public static bool IsOutputRedirected => Console.IsOutputRedirected;

        public static bool IsErrorRedirected => Console.IsErrorRedirected;

        public static TextWriter Out => Console.Out;

        public static TextWriter Error => Console.Error;
    }
}
