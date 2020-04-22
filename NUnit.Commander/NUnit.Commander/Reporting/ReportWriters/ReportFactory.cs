using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public class ReportFactory
    {
        internal ApplicationConfiguration _configuration;
        internal IExtendedConsole _console;
        internal RunContext _runContext;
        internal ColorScheme _colorScheme;

        public ReportFactory(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorScheme colorScheme)
        {
            _configuration = configuration;
            _console = console;
            _runContext = runContext;
            _colorScheme = colorScheme;
        }

        public ColorTextBuilder Create<T>(object parameters = null)
            where T : IReportWriter
        {
            var reportWriter = (T)Activator.CreateInstance(typeof(T), _configuration, _console, _runContext, _colorScheme);
            return reportWriter.Write(parameters);
        }
    }
}
