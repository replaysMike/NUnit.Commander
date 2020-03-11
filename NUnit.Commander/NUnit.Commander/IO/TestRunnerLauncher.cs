using NUnit.Commander.Configuration;
using System;
using System.Diagnostics;
using System.IO;

namespace NUnit.Commander.IO
{
    public class TestRunnerLauncher : IDisposable
    {
        private readonly Options _options;
        private readonly Stream _stream;
        private readonly Stream _streamError;
        private readonly StreamWriter _streamWriter;
        private readonly StreamWriter _streamErrorWriter;
        private Process _process;

        public string ConsoleOutput
        {
            get
            {
                using var reader = new StreamReader(_stream, leaveOpen: true);
                _stream.Seek(0, SeekOrigin.Begin);
                return reader.ReadToEnd();
            }
        }

        public string ConsoleError
        {
            get
            {
                using var reader = new StreamReader(_streamError, leaveOpen: true);
                _streamError.Seek(0, SeekOrigin.Begin);
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Get the exit code for the process
        /// </summary>
        public int ExitCode => _process?.ExitCode ?? 0;

        public TestRunnerLauncher(Options options)
        {
            _options = options;
            _stream = new MemoryStream();
            _streamWriter = new StreamWriter(_stream);
            _streamWriter.AutoFlush = true;
            _streamError = new MemoryStream();
            _streamErrorWriter = new StreamWriter(_streamError);
            _streamErrorWriter.AutoFlush = true;
        }

        /// <summary>
        /// Wait for process to exit
        /// </summary>
        public void WaitForExit() => _process.WaitForExit();

        public bool StartTestRunner()
        {
            var runnerProcess = string.Empty;
            var runnerArguments = _options.TestRunnerArguments;
            var runnerPath = string.Empty;
            var pathToProcess = string.Empty;
            switch (_options.TestRunner)
            {
                case TestRunner.NUnitConsole:
                    runnerProcess = "nunit3-console.exe";
                    // runnerArguments += "";
                    if (!string.IsNullOrEmpty(_options.TestRunnerPath))
                        runnerPath = _options.TestRunnerPath;
                    else
                    {
                        // default nunit-console path
                        runnerPath = @"C:\Program Files (x86)\NUnit.org\nunit-console\";
                    }
                    pathToProcess = Path.Combine(runnerPath, runnerProcess);
                    if (!File.Exists(pathToProcess))
                    {
                        Console.Error.WriteLine($"Error: Unable to find test runner at '{pathToProcess}'");
                        Console.Error.WriteLine($"Please ensure you have specified the correct path.");
                        return false;
                    }
                    break;
                case TestRunner.DotNetTest:
                    runnerProcess = "dotnet";
                    runnerArguments = "test " + runnerArguments;
                    runnerPath = string.Empty;
                    pathToProcess = Path.Combine(runnerPath, runnerProcess);
                    break;
            }
            /*Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{pathToProcess} ");
            Console.WriteLine(runnerArguments);
            Console.ForegroundColor = ConsoleColor.Gray;*/

            Console.WriteLine($"Launching [{_options.TestRunner}] test runner...");
            // launch process
            _process = new Process
            {
                StartInfo = {
                    FileName = pathToProcess,
                    Arguments = runnerArguments,
                },
                EnableRaisingEvents = true,
            };
            if (!_options.EnableDisplayOutput)
            {
                // disable all output (default)
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.RedirectStandardError = true;
                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.CreateNoWindow = true;
                _process.EnableRaisingEvents = true;
                _process.OutputDataReceived += Process_OutputDataReceived;
                _process.ErrorDataReceived += Process_ErrorDataReceived;
            }
            _process.Start();
            if (!_options.EnableDisplayOutput)
            {
                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();
            }
            return true;
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            _streamErrorWriter.WriteLine(e.Data);
            // Console.Error.WriteLine(e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            _streamWriter.WriteLine(e.Data);
            // Console.WriteLine(e.Data);
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
                _streamWriter?.Flush();
                _streamErrorWriter?.Flush();
                _streamWriter?.Dispose();
                _streamErrorWriter?.Dispose();
                _stream?.Dispose();
                _streamError?.Dispose();
                _process?.Dispose();
            }
        }
    }
}
