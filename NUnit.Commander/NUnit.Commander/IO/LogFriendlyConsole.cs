using AnyConsole;
using NUnit.Commander.Display;
using System;
using System.Drawing;
using System.Text;
using Console = NUnit.Commander.Display.CommanderConsole;
using ColorfulConsole = Colorful.Console;
using System.Threading;

namespace NUnit.Commander.IO
{
    public class LogFriendlyConsole : IExtendedConsole
    {
        public delegate void KeyPress(KeyPressEventArgs e);
        public event KeyPress OnKeyPress;
        private Thread _inputThread;
        private ManualResetEvent _closeEvent = new ManualResetEvent(false);

        public ConsoleOptions Options { get; set; }

        /// <summary>
        /// True if stdout is redirected
        /// </summary>
        public bool IsOutputRedirected => Console.IsOutputRedirected;

        /// <summary>
        /// True if stderr is redirected
        /// </summary>
        public bool IsErrorRedirected => Console.IsErrorRedirected;

        public Encoding OutputEncoding
        {
            get { return Console.OutputEncoding; }
            set { Console.OutputEncoding = value; }
        }

        public LogFriendlyConsole(bool clearConsole, ColorManager colorScheme)
        {
            Console.SetColorManager(colorScheme);

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            if (!IsOutputRedirected)
            {
                Console.CursorVisible = false;
                if (clearConsole)
                {
                    Console.ResetColor();
                    if (colorScheme.Background.HasValue)
                        Console.BackgroundColor = Color.Black;
                    Console.Clear();
                }

                _inputThread = new Thread(new ThreadStart(InputThread));
                _inputThread.IsBackground = true;
                _inputThread.Start();
            }
        }

        private void InputThread()
        {
            while (!_closeEvent.WaitOne(30))
            {
                if (!Console.IsOutputRedirected)
                {
                    if (System.Console.KeyAvailable)
                    {
                        var key = System.Console.ReadKey(true);
                        ControlKeyState keyState = 0;
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                            keyState |= ControlKeyState.LEFT_CTRL_PRESSED;
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Alt))
                            keyState |= ControlKeyState.LEFT_ALT_PRESSED;
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                            keyState |= ControlKeyState.SHIFT_PRESSED;
                        OnKeyPress?.Invoke(new KeyPressEventArgs(key.Key, keyState));
                    }
                }
            }
        }

        public void SetCursorPosition(int x, int y)
        {
            Console.SetCursorPosition(x, y);
        }

        public void Clear()
        {
        }

        public int ClearAt(int x, int y)
        {
            if (!IsOutputRedirected)
            {
                Console.SetCursorPosition(x, y);
                Console.Write(new string(' ', Console.WindowWidth - x));
            }
            return 0;
        }

        public int ClearAtRange(int startX, int startY, int endX, int endY)
        {
            if (!IsOutputRedirected)
            {
                for (var y = startY; y < endY; y++)
                {
                    Console.SetCursorPosition(startX, y);
                    Console.Write(new string(' ', Console.WindowWidth - startX - endX));
                }
            }
            return 0;
        }

        public void Close()
        {
            // do nothing
        }

        public void Configure(Action<ExtendedConsoleConfiguration> config)
        {
        }

        public string ReadLine() => Console.ReadLine();

        public void Start()
        {
            throw new NotSupportedException();
        }

        public void WaitForClose()
        {
            // do nothing
        }

        public void Write(string text) => Console.Write(text);

        public void Write(StringBuilder text) => Console.Write(text.ToString());

        public void Write(ColorTextBuilder textBuilder) => WriteColorTextBuilder(textBuilder, Console.Write);

        public void WriteAscii(string text) => ColorfulConsole.WriteAscii(text);

        public void WriteAscii(StringBuilder text) => ColorfulConsole.WriteAscii(text.ToString());

        public void WriteAscii(ColorTextBuilder textBuilder) => WriteColorTextBuilder(textBuilder, ColorfulConsole.WriteAscii);

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
            var totalLength = 0;
            foreach (var text in textBuilder.TextFragments)
            {
                if (text.ForegroundColor.HasValue)
                    Console.ForegroundColor = text.ForegroundColor.Value;
                else
                    Console.ForegroundColor = defaultForegroundColor;
                if (text.BackgroundColor.HasValue)
                    Console.BackgroundColor = text.BackgroundColor.Value;
                if (textBuilder.MaxLength.HasValue && totalLength + text.Text.Length > textBuilder.MaxLength.Value)
                {
                    var txt = text.Text.Substring(0, (totalLength + text.Text.Length) - textBuilder.MaxLength.Value);
                    writeAction.Invoke(txt);
                    break;
                }
                writeAction.Invoke(text.Text);
                totalLength += text.Text.Length;
            }
            Console.ForegroundColor = defaultForegroundColor;
        }

        public void WriteRow(string rowName, Component component, ColumnLocation location)
        {
            throw new NotSupportedException();
        }

        public void WriteRow(string rowName, Component component, ColumnLocation location, Color foreColor)
        {
            throw new NotSupportedException();
        }

        public void WriteRow(string rowName, Component component, ColumnLocation location, Color foreColor, Color backColor)
        {
            throw new NotSupportedException();
        }

        public void WriteRow(string rowName, string text, ColumnLocation location, Color foreColor)
        {
            throw new NotSupportedException();
        }

        public void WriteRow(string rowName, string text, ColumnLocation location, Color foreColor, Color backColor)
        {
            throw new NotSupportedException();
        }

        public void WriteRow(string rowName, string text, ColumnLocation location)
        {
            throw new NotSupportedException();
        }

        public void WriteRow(string rowName, string text, ColumnLocation location, int offset)
        {
            throw new NotSupportedException();
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
                _closeEvent?.Set();
                if (_inputThread?.Join(5 * 1000) == false)
                    _inputThread?.Abort();
                Console.Out.Flush();
                Console.Error.Flush();
                if (!IsOutputRedirected)
                {
                    Console.CursorVisible = true;
                }
            }
        }
    }
}
