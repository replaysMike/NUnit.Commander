using ProtoBuf;
using System;

namespace NUnit.Commander.Models
{
    [ProtoContract]
    public class TestHistoryEntry
    {
        [ProtoMember(1)]
        public string FullName { get; set; }
        [ProtoMember(2)]
        public string CommanderRunId { get; set; }
        [ProtoMember(3)]
        public string TestRunId { get; set; }
        [ProtoMember(4)]
        public bool IsPass { get; set; }
        [ProtoMember(5)]
        public TimeSpan Duration { get; set; }
        [ProtoMember(6)]
        public DateTime TestDate { get; set; }

        public TestHistoryEntry() { }
        public TestHistoryEntry(string commanderRunId, string testRunId, TestCaseReport e)
        {
            FullName = e.FullName;
            CommanderRunId = commanderRunId;
            TestRunId = testRunId;
            IsPass = e.TestResult;
            Duration = e.Duration;
            TestDate = e.EndTime;
        }

        public override string ToString() => FullName;
    }
}
