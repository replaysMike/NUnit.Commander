using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace NUnit.Commander.IO
{
    /// <summary>
    /// Wraps an IExtendedConsole to include logging capabilities
    /// </summary>
    public class ConsoleWrapper : IExtendedConsole
    {
        private readonly ApplicationConfiguration _config;
        private readonly Stream _stream;
        private readonly StreamWriter _streamWriter;
        private readonly IExtendedConsole _console;
        private Guid _testRunId;

        public ConsoleWrapper(IExtendedConsole console, ApplicationConfiguration config)
        {
            _console = console;
            _config = config;
            if (config.EnableLog)
            {
                if (EnsurePathIsCreated(config.LogPath))
                {
                    _stream = new MemoryStream();
                }
            }
            _stream ??= Stream.Null; // log to null stream
            _streamWriter = new StreamWriter(_stream);
            _streamWriter.AutoFlush = true;
        }

        private bool EnsurePathIsCreated(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                _console.WriteLine($"Error: Could not create logging path at {path}. {ex.GetBaseException().Message}");
            }
            return false;
        }

        public void RegisterTestRunId(Guid testRunId)
        {
            _testRunId = testRunId;
        }

        public ConsoleOptions Options => _console.Options;

        public bool IsOutputRedirected => _console.IsOutputRedirected;

        public bool IsErrorRedirected => _console.IsErrorRedirected;

        public Encoding OutputEncoding
        {
            get
            {
                return _console.OutputEncoding;
            }
            set
            {
                _console.OutputEncoding = value;
            }
        }

        public int WindowLeft { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int WindowTop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int WindowHeight { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int WindowWidth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int CursorLeft { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int CursorTop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color ForegroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Color BackgroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool CursorVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string FontName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public short FontXSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public short FontYSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int FontWeight { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void SetCursorPosition(int x, int y) => _console.SetCursorPosition(x, y);

        public void Clear() => _console.Clear();

        public int ClearAt(int x, int y) => _console.ClearAt(x, y);

        public int ClearAtRange(int startX, int startY, int endX, int endY) => _console.ClearAtRange(startX, startY, endX, endY);

        public void Close()
        {
            _console.Close();
            _streamWriter?.Flush();
            _streamWriter?.Close();
            _stream?.Close();
        }

        public void Configure(Action<ExtendedConsoleConfiguration> config)
        {
            _console.Configure(config);
        }

        public string ReadLine() => _console.ReadLine();

        public void Start() => _console.Start();

        public void WaitForClose() => _console.WaitForClose();

        public void Write(string text)
        {
            _streamWriter.Write(text);
            _console.Write(text);
        }

        public void Write(StringBuilder text)
        {
            _streamWriter.Write(text.ToString());
            _console.Write(text);
        }

        public void Write(ColorTextBuilder textBuilder)
        {
            _streamWriter.Write(textBuilder.ToString());
            _console.Write(textBuilder);
        }

        public void WriteAscii(string text)
        {
            _streamWriter.Write(text);
            _console.WriteAscii(text);
        }

        public void WriteAscii(StringBuilder text)
        {
            _streamWriter.Write(text.ToString());
            _console.WriteAscii(text);
        }

        public void WriteAscii(ColorTextBuilder textBuilder)
        {
            _streamWriter.Write(textBuilder.ToString());
            _console.WriteAscii(textBuilder);
        }

        public void WriteAt(string text, int xPos, int yPos, DirectOutputMode directOutputMode, Color? foregroundColor = null, Color? backgroundColor = null)
        {
            _console.WriteAt(text, xPos, yPos, directOutputMode, foregroundColor, backgroundColor);
        }

        public void WriteAt(string text, int xPos, int yPos)
        {
            _console.WriteAt(text, xPos, yPos);
        }

        public void WriteAt(ColorTextBuilder textBuilder, int xPos, int yPos, DirectOutputMode directOutputMode)
        {
            _console.WriteAt(textBuilder, xPos, yPos, directOutputMode);
        }

        public void WriteLine()
        {
            _streamWriter.WriteLine();
            _console.WriteLine();
        }

        public void WriteLine(string text)
        {
            _streamWriter.WriteLine(text);
            _console.WriteLine(text);
        }

        public void WriteLine(StringBuilder text)
        {
            _streamWriter.WriteLine(text.ToString());
            _console.WriteLine(text.ToString());
        }

        public void WriteLine(ColorTextBuilder textBuilder)
        {
            _streamWriter.WriteLine(textBuilder.ToString());
            _console.WriteLine(textBuilder);
        }

        public void WriteRaw(string text)
        {
            _streamWriter.WriteLine(text);
            _console.WriteRaw(text);
        }

        public void WriteRow(string rowName, Component component, ColumnLocation location) => _console.WriteRow(rowName, component, location);

        public void WriteRow(string rowName, Component component, ColumnLocation location, Color foreColor) => _console.WriteRow(rowName, component, location, foreColor);

        public void WriteRow(string rowName, Component component, ColumnLocation location, Color foreColor, Color backColor) => _console.WriteRow(rowName, component, location, foreColor, backColor);

        public void WriteRow(string rowName, string text, ColumnLocation location, Color foreColor) => _console.WriteRow(rowName, text, location, foreColor);

        public void WriteRow(string rowName, string text, ColumnLocation location, Color foreColor, Color backColor) => _console.WriteRow(rowName, text, location, foreColor, backColor);

        public void WriteRow(string rowName, string text, ColumnLocation location) => _console.WriteRow(rowName, text, location);

        public void WriteRow(string rowName, string text, ColumnLocation location, int offset) => _console.WriteRow(rowName, text, location, offset);


        public bool CheckIfCharInFont(char character, Font font) => _console.CheckIfCharInFont(character, font);

        public ICollection<ExtendedConsole.FontRange> GetFontUnicodeRanges(Font font) => _console.GetFontUnicodeRanges(font);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _streamWriter?.Flush();

                if (_config.EnableLog)
                {
                    var logFilename = Path.Combine(_config.LogPath, $"{_testRunId}.log");
                    using var fileStream = new FileStream(logFilename, FileMode.OpenOrCreate, FileAccess.Write);
                    _stream.Seek(0, SeekOrigin.Begin);
                    _stream.CopyTo(fileStream);
                }

                _streamWriter?.Dispose();
                _stream?.Dispose();
            }
        }
    }
}
