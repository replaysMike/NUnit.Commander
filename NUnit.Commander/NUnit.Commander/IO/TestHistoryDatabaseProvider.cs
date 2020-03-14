using NUnit.Commander.Configuration;
using NUnit.Commander.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace NUnit.Commander.IO
{
    /// <summary>
    /// Provides a test history datatabase
    /// </summary>
    public class TestHistoryDatabaseProvider : IDisposable
    {
        private const string Filename = "NUnitCommander_TestReliability.db";
        private readonly ApplicationConfiguration _configuration;
        private TestHistoryDatabase _db;
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private bool _hasChanges;
        public TestHistoryDatabase Database
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _db;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public TestHistoryDatabaseProvider(ApplicationConfiguration configuration)
        {
            _configuration = configuration;
            _db = new TestHistoryDatabase();
        }

        public void AddTestHistory(TestHistoryEntry entry)
        {
            _lock.EnterWriteLock();
            try
            {
                _db.Entries.Add(entry);
                TruncateLogInternal();
                _hasChanges = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void AddTestHistoryRange(IEnumerable<TestHistoryEntry> range)
        {
            _lock.EnterWriteLock();
            try
            {
                _db.Entries.AddRange(range);
                TruncateLogInternal();
                _hasChanges = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void LoadDatabase()
        {
            var path = Path.Combine(_configuration.LogPath, Filename);
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                using var stream = new MemoryStream(bytes);
                _lock.EnterWriteLock();
                try
                {
                    _db = Serializer.Deserialize<TestHistoryDatabase>(stream);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        public void SaveDatabase()
        {
            if (_hasChanges)
            {
                var path = Path.Combine(_configuration.LogPath, Filename);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using var stream = new FileStream(path, FileMode.Create);
                _lock.EnterReadLock();
                try
                {
                    // order by oldest last
                    _db.Entries = _db.Entries.OrderBy(x => x.TestDate).ToList();
                    Serializer.Serialize(stream, _db);
                    _hasChanges = false;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Delete all test history entries
        /// </summary>
        public void DeleteAll()
        {
            var path = Path.Combine(_configuration.LogPath, Filename);
            File.Delete(path);
        }

        public void TruncateLog()
        {
            _lock.EnterWriteLock();
            try
            {
                TruncateLogInternal();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void TruncateLogInternal()
        {
            _db.Entries = _db.Entries
                // order by oldest first
                .OrderByDescending(x => x.TestDate)
                // group by run
                .GroupBy(x => x.CommanderRunId)
                // take the X entries we want
                .Take(_configuration.HistoryAnalysisConfiguration.MaxTestReliabilityRuns)
                // ungroup
                .SelectMany(x => x.ToList())
                // sort by oldest last
                .OrderBy(x => x.TestDate)
                .ToList();
            _hasChanges = true;
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
                _lock?.EnterWriteLock();
                try
                {
                    _db?.Entries?.Clear();
                    _db = null;
                }
                finally
                {
                    _lock?.ExitWriteLock();
                    _lock?.Dispose();
                    _lock = null;
                }
            }
        }
    }
}
