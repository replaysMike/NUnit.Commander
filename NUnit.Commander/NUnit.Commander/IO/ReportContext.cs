using System;
using System.Collections.Generic;
using System.Text;

namespace NUnit.Commander.IO
{
    public class ReportContext
    {
        public Guid CommanderRunId { get; set; }
        public ICollection<Guid> TestRunIds { get; set; } = new List<Guid>();
        public ICollection<string> Frameworks { get; set; } = new List<string>();
        public ICollection<string> FrameworkRuntimes { get; set; } = new List<string>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
