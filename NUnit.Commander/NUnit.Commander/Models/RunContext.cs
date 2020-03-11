using NUnit.Commander.IO;
using System.Collections.Generic;

namespace NUnit.Commander.Models
{
    public class RunContext
    {
        public Dictionary<ReportContext, ICollection<DataEvent>> Runs { get; set; } = new Dictionary<ReportContext, ICollection<DataEvent>>();
    }
}
