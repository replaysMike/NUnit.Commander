using NUnit.Commander.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NUnit.Commander.IO
{
    public class TestRunnerLauncher : IDisposable
    {
        public class PendingProcess
        {
            private ProcessJobTracker _jobTracker;
            private bool _enableDisplayOutput;
            public bool IsStarted { get; set; }
            public bool IsComplete { get; set; }
            public int Id { get; set; }
            public Process Process { get; set; }
            
            public PendingProcess(Process process, ProcessJobTracker jobTracker, bool enableDisplayOutput)
            {
                IsStarted = false;
                IsComplete = false;
                Process = process;
                _jobTracker = jobTracker;
                _enableDisplayOutput = enableDisplayOutput;
            }

            public void Start()
            {
                IsStarted = true;
                if (Process.Start())
                {
                    Id = Process.Id;

                    // tell windows to close this process if Commander is closed
                    _jobTracker.AddProcess(Process);
                }
                if (!_enableDisplayOutput)
                {
                    Process.BeginErrorReadLine();
                    Process.BeginOutputReadLine();
                }
            }
        }

        private readonly Options _options;
        private readonly Stream _stream;
        private readonly Stream _streamError;
        private readonly StreamWriter _streamWriter;
        private readonly StreamWriter _streamErrorWriter;
        private readonly ProcessJobTracker _jobTracker;
        private int _currentProcessIndex = -1;
        private List<PendingProcess> _processes = new List<PendingProcess>();

        public event EventHandler OnTestRunnerExit;
        public Options Options => _options;
        public TestRunner TestRunnerName => _options.TestRunner.Value;
        public bool HasErrors => !string.IsNullOrEmpty(ConsoleError.Replace(Environment.NewLine, string.Empty).Replace("\t", string.Empty).Trim());
        public int ProcessCount => _processes.Count;

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
        public int ExitCode
        {
            get
            {
                var exitCode = 0;
                try
                {
                    var process = _processes.FirstOrDefault();
                    exitCode = process?.Process.ExitCode ?? 0;
                }
                catch (Exception)
                {
                    // exceptions are ok
                }
                return exitCode;
            }

        }

        public TestRunnerLauncher(Options options)
        {
            _options = options;
            _stream = new MemoryStream();
            _streamWriter = new StreamWriter(_stream);
            _streamWriter.AutoFlush = true;
            _streamError = new MemoryStream();
            _streamErrorWriter = new StreamWriter(_streamError);
            _streamErrorWriter.AutoFlush = true;
            _jobTracker = new ProcessJobTracker();
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                _streamErrorWriter.WriteLine(e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                _streamWriter.WriteLine(e.Data);
        }

        /// <summary>
        /// Start the next process in the queue
        /// </summary>
        /// <returns></returns>
        public bool NextProcess()
        {
            if(_currentProcessIndex < _processes.Count - 1)
            {
                _currentProcessIndex++;
                _processes[_currentProcessIndex].Start();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Wait for process to exit
        /// </summary>
        public void WaitForExit()
        {
            try
            {
                foreach (var process in _processes)
                {
                    process?.Process.WaitForExit();
                    process.IsComplete = true;
                }
            }
            catch (COMException)
            {
                // the process ended and handle was already released
            }
        }

        public void Kill()
        {
            try
            {
                foreach (var process in _processes)
                    process?.Process?.Kill(true);
            }
            catch (Exception)
            {
                // exceptions are ok, process is already ended
            }
        }

        public bool StartTestRunner()
        {
            var isSuccess = false;
            switch (_options.TestRunner)
            {
                case TestRunner.NUnitConsole:
                    isSuccess = LaunchNUnitConsole(string.Join(" ", _options.TestRunnerArguments, _options.NUnitConsoleArguments, _options.TestAssemblies));
                    break;
                case TestRunner.DotNetTest:
                    isSuccess = LaunchDotNetTest(string.Join(" ", _options.TestRunnerArguments, _options.DotNetTestArguments, _options.TestAssemblies));
                    break;
                case TestRunner.Auto:
                    // launch runner for each framework detected
                    var assemblyMetadata = DetectRunnersFromAssemblies(_options.TestAssemblies);
                    var dotNetFrameworkTestAssemblies = assemblyMetadata.Where(x => x.Value == FrameworkType.DotNetFramework);
                    var dotNetCoreTestAssemblies = assemblyMetadata.Where(x => x.Value == FrameworkType.DotNetCore);
                    if (dotNetFrameworkTestAssemblies.Any())
                    {
                        var nunitConsoleTestAssemblies = string.Join(" ", dotNetFrameworkTestAssemblies.Select(x => x.Key));
                        isSuccess = LaunchNUnitConsole(string.Join(" ", _options.NUnitConsoleArguments, nunitConsoleTestAssemblies));
                    }
                    if (dotNetCoreTestAssemblies.Any())
                    {
                        var dotNetTestAssemblies = string.Join(" ", dotNetCoreTestAssemblies.Select(x => x.Key));
                        isSuccess = LaunchDotNetTest(string.Join(" ", dotNetTestAssemblies, _options.DotNetTestArguments));
                    }
                    //System.Threading.Thread.Sleep(500);
                    break;
            }
            /*Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{pathToProcess} ");
            Console.WriteLine(runnerArguments);
            Console.ForegroundColor = ConsoleColor.Gray;*/

            // launch process
            //_processes.First().Start();
            return isSuccess;
        }

        private bool LaunchNUnitConsole(string runnerArguments)
        {
            Console.WriteLine($"Launching [NUnitConsole] test runner...");
            var runnerProcess = "nunit3-console.exe";
            // default nunit-console path
            var runnerPath = @"C:\Program Files (x86)\NUnit.org\nunit-console\";
            // runnerArguments += "";
            if (!string.IsNullOrEmpty(_options.TestRunnerPath))
                runnerPath = _options.TestRunnerPath;
            var pathToProcess = Path.Combine(runnerPath, runnerProcess);
            if (!File.Exists(pathToProcess))
            {
                Console.Error.WriteLine($"Error: Unable to find test runner at '{pathToProcess}'");
                Console.Error.WriteLine($"Please ensure you have specified the correct path.");
                return false;
            }
            LaunchProcess(pathToProcess, runnerArguments);
            return true;
        }

        private bool LaunchDotNetTest(string runnerArguments)
        {
            Console.WriteLine($"Launching [dotnet] test runner...");
            var runnerProcess = "dotnet";
            var testRunnerArguments = "test " + runnerArguments;
            var runnerPath = string.Empty;
            var pathToProcess = Path.Combine(runnerPath, runnerProcess);
            LaunchProcess(pathToProcess, testRunnerArguments);
            return true;
        }

        private void LaunchProcess(string pathToProcess, string runnerArguments)
        {
            var process = new Process
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
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
            }

            process.Exited += Process_Exited;
            _processes.Add(new PendingProcess(process, _jobTracker, _options.EnableDisplayOutput));
            
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            var senderProcess = (Process)sender;
            var process = _processes.FirstOrDefault(x => x.Id == senderProcess.Id);
            if (process != null)
            {
                process.IsComplete = true;
            }
            if (_processes.Count(x => x.IsComplete) == _processes.Count)
                OnTestRunnerExit?.Invoke(this, e);
        }

        /// <summary>
        /// For each assembly specified, detect the .net runtime the dll is targeting
        /// </summary>
        /// <param name="runnerAssemblies"></param>
        /// <returns></returns>
        private Dictionary<string, FrameworkType> DetectRunnersFromAssemblies(string runnerAssemblies)
        {
            var assemblyMetadata = new Dictionary<string, FrameworkType>();
            // load all of the assemblies and inspect them
            foreach (var assemblyPath in runnerAssemblies.Split(" ", StringSplitOptions.RemoveEmptyEntries))
            {
                assemblyMetadata.Add(assemblyPath, PortableExecutableHelper.GetAssemblyFrameworkType(assemblyPath));
            }
            return assemblyMetadata;
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
                try
                {
                    foreach(var process in _processes)
                        process?.Process.Dispose();
                    _processes.Clear();
                }
                catch (Exception)
                {
                    // exceptions are ok
                }
                _jobTracker?.Dispose();
            }
        }
    }
}
