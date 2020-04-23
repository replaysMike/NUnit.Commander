using NUnit.Commander.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

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
        public static void PerformUpdate()
        {
            try
            {
                // download the update
                // replace the current exe
                // relaunch
                var filename = Path.Combine(Path.GetTempPath(), "NUnit.Commander\\NUnit.Commander.msi");
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                var assemblyFile = Assembly.GetExecutingAssembly().Location;
                var assemblyPath = Path.GetDirectoryName(assemblyFile);
                var assemblyTempFile = $"{assemblyFile}.tmp";
                var client = new HttpClient();
                if (!Console.IsOutputRedirected)
                {
                    Console.CursorVisible = false;
                    Console.WriteLine($"Downloading v{LatestVersion.ToString()} update...");
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
                            Console.SetCursorPosition(0, Console.CursorTop);
                            Console.Write($"{percDone:n0}%     ");
                        }
                    } while (bytesRead > 0);
                    streamWriter.Close();
                    streamWriter.Dispose();
                    if (!Console.IsOutputRedirected)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.WriteLine($"Download complete.     ");
                    }
                }).GetAwaiter().GetResult();

                if (!Console.IsOutputRedirected)
                    Console.Write($"Installing v{LatestVersion.ToString()} update... ");

                // rename the current assembly
                if (File.Exists(assemblyTempFile))
                    File.Delete(assemblyTempFile);
                File.Move(assemblyFile, assemblyTempFile);

                var process = new Process();
                process.StartInfo.FileName = "msiexec";
                process.StartInfo.WorkingDirectory = Path.GetTempPath();
                process.StartInfo.Arguments = $" /passive /norestart /log c:\\logs\\nunitinstall.log /i {filename} INSTALLFOLDER={assemblyPath}";
                process.StartInfo.Verb = "runas";
                process.Start();
                var exited = process.WaitForExit(30 * 1000);

                if (!Console.IsOutputRedirected)
                {
                    Console.WriteLine($"done! Exit code: {process.ExitCode}");
                    try
                    {
                        Process.Start(assemblyFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to restart application automatically. Please launch application. {ex.Message}");
                    }

                    // start a new process with a slight delay to cleanup our temp assembly file
                    Process.Start(new ProcessStartInfo()
                    {
                        Arguments = "/C choice /C Y /N /D Y /T 2 & Del \"" + assemblyTempFile + "\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        FileName = "cmd.exe"
                    });
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
