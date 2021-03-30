using NUnit.Commander.Display;
using NUnit.Commander.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Console = NUnit.Commander.Display.CommanderConsole;

namespace NUnit.Commander.AutoUpdate
{
    /// <summary>
    /// Manages auto-updates
    /// </summary>
    public static class AutoUpdater
    {
        private const string _apiEndpoint = "https://img.shields.io/github/v/release";
        private const string _repo = "replaysMike/NUnit.Commander";

        public static Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        public static Version LatestVersion;

        /// <summary>
        /// Check for an application version update
        /// </summary>
        /// <returns></returns>
        public static bool CheckForUpdate()
        {
            var hasUpdate = false;
            var client = new HttpClient();
            Task.Run(async () =>
            {
                var uri = $"{_apiEndpoint}/{_repo}";
                var response = await client.GetAsync(uri);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var xml = new XmlDocument();
                    xml.LoadXml(content);
                    var latestVersionText = xml.DocumentElement.LastChild.LastChild.InnerText;
                    LatestVersion = Version.Parse(latestVersionText.Replace("v", ""));
                    if (LatestVersion > CurrentVersion)
                    {
                        hasUpdate = true;
                    }
                }
            }).GetAwaiter().GetResult();

            return hasUpdate;
        }

        /// <summary>
        /// Update the current application
        /// </summary>
        public static void PerformUpdate(Options options, ColorScheme colorScheme)
        {
            try
            {
                // download the update
                // replace the current exe
                // relaunch
                var filename = Path.Combine(Path.GetTempPath(), "NUnit.Commander\\NUnit.Commander.msi");
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

                // this won't work with .net core single file publishing
                //var assemblyFile = Assembly.GetExecutingAssembly().Location;
                var currentProcess = Process.GetCurrentProcess();
                var assemblyFile = currentProcess.MainModule.FileName;
#if DEBUG
                var assemblyArgs = Environment.CommandLine.Replace(assemblyFile.Replace(".exe", ".dll"), "");
#else
                var assemblyArgs = Environment.CommandLine.Replace(assemblyFile, "");
#endif
                var assemblyPath = Path.GetDirectoryName(assemblyFile);
                var assemblyTempFile = $"{assemblyFile}.tmp";
                var client = new HttpClient();
                var percentageX = 0;
                if (!Console.IsOutputRedirected)
                {
                    Console.CursorVisible = false;
                    Console.Write($"Downloading v{LatestVersion.ToString()} update... ");
                    percentageX = Console.CursorLeft;
                }
                Task.Run(async () =>
                {
                    var response = await client.GetAsync($"https://github.com/replaysMike/NUnit.Commander/releases/latest/download/NUnit.Commander.msi", HttpCompletionOption.ResponseHeadersRead);
                    var stream = await response.Content.ReadAsStreamAsync();
                    var totalLength = response.Content.Headers.ContentLength;
                    var buffer = new byte[512 * 1024]; // 512kb buffer
                    int bytesRead;
                    var streamWriter = new FileStream(filename, FileMode.Create);
                    do
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        streamWriter.Write(buffer, 0, bytesRead);
                        var percDone = ((double)streamWriter.Length / totalLength) * 100.0;
                        if (!Console.IsOutputRedirected)
                        {
                            Console.SetCursorPosition(percentageX, Console.CursorTop);
                            Console.Write($"{percDone:n0}%     ", colorScheme.Duration);
                        }
                    } while (bytesRead > 0);
                    streamWriter.Close();
                    streamWriter.Dispose();
                    if (!Console.IsOutputRedirected)
                        Console.WriteLine($"{Environment.NewLine}Download complete.     ", colorScheme.Duration);
                }).GetAwaiter().GetResult();

                if (!Console.IsOutputRedirected)
                    Console.Write($"Installing v{LatestVersion.ToString()} update ({assemblyPath})... ");

                // rename the current assembly
                if (File.Exists(assemblyTempFile))
                    File.Delete(assemblyTempFile);
                File.Move(assemblyFile, assemblyTempFile);

                var process = new Process();
                process.StartInfo.FileName = "msiexec";
                process.StartInfo.WorkingDirectory = Path.GetTempPath();
                process.StartInfo.Arguments = $"/passive /norestart /i {filename} INSTALLFOLDER={assemblyPath}";
                // To enable msi installation logging use: /log c:\\logs\\commander-msi-install.log
                process.StartInfo.Verb = "runas";
                process.Start();
                //var exited = process.WaitForExit(30 * 1000);

                if (!Console.IsOutputRedirected)
                {
                    Console.WriteLine($"done!");
                    if (options.Relaunch)
                    {
                        // relaunch application with same arguments. Note this will be out of process
                        try
                        {
                            var relaunchProcess = new Process();
                            relaunchProcess.StartInfo.FileName = assemblyFile;
                            relaunchProcess.StartInfo.Arguments = assemblyArgs;
                            relaunchProcess.Start();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to restart application automatically. Please re-launch application manually. Error: {ex.Message}");
                        }
                    }
                    // start a new process with a slight delay to cleanup our temp assembly file
                    Process.Start(new ProcessStartInfo()
                    {
                        Arguments = "/C choice /C Y /N /D Y /T 2 & Del \"" + assemblyTempFile + "\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        FileName = "cmd.exe"
                    });

                    // return success updated
                    Environment.Exit((int)ExitCode.ApplicationUpdated);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update application. {ex.Message}");
                // continue with run
            }
        }
    }
}
