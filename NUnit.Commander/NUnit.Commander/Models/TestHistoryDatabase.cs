using ProtoBuf;
using System.Collections.Generic;

namespace NUnit.Commander.Models
{
    /// <summary>
    /// Test history database
    /// </summary>
    [ProtoContract]
    public class TestHistoryDatabase
    {
        /// <summary>
        /// History of test entries
        /// </summary>
        [ProtoMember(1)]
        public List<TestHistoryEntry> Entries { get; internal set; } = new List<TestHistoryEntry>();
    }
}
