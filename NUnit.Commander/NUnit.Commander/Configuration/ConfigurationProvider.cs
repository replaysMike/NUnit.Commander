using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;

namespace NUnit.Commander.Configuration
{
    public class ConfigurationProvider
    {
        public IConfiguration LoadConfiguration(string configFilename = "appsettings.json")
        {
            // for .net core single file publishing, this must be used to locate the config
            var currentProcess = Process.GetCurrentProcess();
            var configPath = Path.GetDirectoryName(currentProcess.MainModule.FileName);
            var configFile = Path.Combine(configPath, configFilename);
            // Console.WriteLine($"Loading configuration from {configFile}");
            if (!File.Exists(configFile))
            {
                // when profiling the paths will be incorrect, use the base directory of the app instead
                configPath = AppDomain.CurrentDomain.BaseDirectory;
                configFile = Path.Combine(configPath, configFilename);
                // Console.WriteLine($"Loading configuration from {configFile}");
            }

            if (!File.Exists(configFile))
                throw new FileNotFoundException($"The configuration file named '{configFile}' was not found.");
            var builder = new ConfigurationBuilder()
                .SetBasePath(configPath)
                .AddJsonFile(configFilename, optional: false);
            return builder.Build();
        }

        public T Get<T>(IConfiguration configuration)
        {
            return configuration.GetSection(typeof(T).Name).Get<T>();
        }
    }
}
