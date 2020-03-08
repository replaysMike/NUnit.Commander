using AnyConsole;
using System;
using System.Drawing;
using System.Text;
using Console = Colorful.Console;

namespace NUnit.Commander.IO
{
    public class LogFriendlyConsole : IExtendedConsole
    {
        public ConsoleOptions Options { get; set; }

        /// <summary>
        /// True if stdout is redirected
        /// </summary>
        public bool IsOutputRedirected => Console.IsOutputRedirected;

        /// <summary>
        /// True if stderr is redirected
        /// </summary>
        public bool IsErrorRedirected => Console.IsErrorRedirected;

        public LogFriendlyConsole()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.ForegroundColor = Color.Gray;
            Console.BackgroundColor = Color.Black;
            if (!IsOutputRedirected)
            {
                Console.CursorVisible = false;
                Console.Clear();
            }
        }

        public void Clear()
        {
        }

        public int ClearAt(int x, int y)
        {
            if (!IsOutputRedirected)
                Console.SetCursorPosition(x, y);
            Console.Write(new string(' ', Console.WindowWidth - x));
            return 0;
        }

        public int ClearAtRange(int startX, int startY, int endX, int endY)
        {
            for(var y = startY; y < endY; y++)
            {
                if (!IsOutputRedirected)
                    Console.SetCursorPosition(startX, y);
                Console.Write(new string(' ', Console.WindowWidth - startX - endX));
            }
            return 0;
        }

        public void Close()
        {
        }

        public void Configure(Action<ExtendedConsoleConfiguration> config)
        {
        }

        public string ReadLine() => Console.ReadLine();

        public void Start()
        {
        }

        public void WaitForClose()
        {
        }

        public void Write(string text) => Console.Write(text);

        public void Write(StringBuilder text) => Console.Write(text.ToString());

        public void Write(ColorTextBuilder textBuilder) => WriteColorTextBuilder(textBuilder, Console.Write);

        public void WriteAscii(string text) => Console.WriteAscii(text);

        public void WriteAscii(StringBuilder text) => Console.WriteAscii(text.ToString());

        public void WriteAscii(ColorTextBuilder textBuilder) => WriteColorTextBuilder(textBuilder, Console.WriteAscii);

        public void WriteAt(string text, int xPos, int yPos, DirectOutputMode directOutputMode, Color? foregroundColor = null, Color? backgroundColor = null)
        {
            if (!IsOutputRedirected)
                Console.SetCursorPosition(xPos, yPos);
            Console.Write(text);
        }

        public void WriteAt(string text, int xPos, int yPos)
        {
            if (!IsOutputRedirected)
                Console.SetCursorPosition(xPos, yPos);
            Console.Write(text);
        }

        public void WriteAt(ColorTextBuilder textBuilder, int xPos, int yPos, DirectOutputMode directOutputMode)
        {
            if (!IsOutputRedirected)
                Console.SetCursorPosition(xPos, yPos);
            WriteColorTextBuilder(textBuilder, Console.Write);
        }

        public void WriteLine() => Console.WriteLine();

        public void WriteLine(string text) => Console.WriteLine(text);

        public void WriteLine(StringBuilder text) => Console.WriteLine(text.ToString());

        public void WriteLine(ColorTextBuilder textBuilder) => WriteColorTextBuilder(textBuilder, Console.Write); // Console.Write is correct, line endings must come from ColorTextBuilder

        public void WriteRaw(string text) => Console.WriteLine(text);

        private void WriteColorTextBuilder(ColorTextBuilder textBuilder, Action<string> writeAction)
        {
            var defaultForegroundColor = Console.ForegroundColor;
            var defaultBackgroundColor = Console.BackgroundColor;
            foreach (var text in textBuilder.TextFragments)
            {
                if (text.ForegroundColor.HasValue)
                    Console.ForegroundColor = text.ForegroundColor.Value;
                else
                    Console.ForegroundColor = defaultForegroundColor;
                if (text.BackgroundColor.HasValue)
                    Console.BackgroundColor = text.BackgroundColor.Value;
                else
                    Console.BackgroundColor = defaultBackgroundColor;
                writeAction.Invoke(text.Text);
            }
            Console.ForegroundColor = defaultForegroundColor;
            Console.BackgroundColor = defaultBackgroundColor;
        }

        public void WriteRow(string rowName, Component component, ColumnLocation location)
        {
            throw new NotImplementedException();
        }

        public void WriteRow(string rowName, Component component, ColumnLocation location, Color foreColor)
        {
            throw new NotImplementedException();
        }

        public void WriteRow(string rowName, Component component, ColumnLocation location, Color foreColor, Color backColor)
        {
            throw new NotImplementedException();
        }

        public void WriteRow(string rowName, string text, ColumnLocation location, Color foreColor)
        {
            throw new NotImplementedException();
        }

        public void WriteRow(string rowName, string text, ColumnLocation location, Color foreColor, Color backColor)
        {
            throw new NotImplementedException();
        }

        public void WriteRow(string rowName, string text, ColumnLocation location)
        {
            throw new NotImplementedException();
        }

        public void WriteRow(string rowName, string text, ColumnLocation location, int offset)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                Console.Out.Flush();
                Console.Error.Flush();
            }
        }
    }
}
