using NUnit.Commander.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Commander.Reporting
{
    public class PerformanceLog : IDisposable
    {
        internal const int PeakSampleTimeSeconds = 15;

        private Dictionary<PerformanceType, IList<PerformanceEntry>> _log = new Dictionary<PerformanceType, IList<PerformanceEntry>>();

        public PerformanceLog()
        {
            _log.Add(PerformanceType.CpuUsed, new List<PerformanceEntry>());
            _log.Add(PerformanceType.MemoryUsed, new List<PerformanceEntry>());
            _log.Add(PerformanceType.DiskTime, new List<PerformanceEntry>());
            _log.Add(PerformanceType.TestConcurrency, new List<PerformanceEntry>());
            _log.Add(PerformanceType.TestFixtureConcurrency, new List<PerformanceEntry>());
            _log.Add(PerformanceType.AssemblyConcurrency, new List<PerformanceEntry>());
        }

        public void AddEntry(PerformanceType type, float value)
        {
            System.Diagnostics.Debug.WriteLine($"{type}: {value}");
            _log[type].Add(new PerformanceEntry(value));
        }

        public IList<PerformanceEntry> GetAll(PerformanceType type)
        {
            return _log[type];
        }

        public double GetMedian(PerformanceType type)
        {
            if (_log[type].Count > 0)
                return _log[type].Median(x => x.Value);
            return 0;
        }

        public double GetAverage(PerformanceType type)
        {
            if (_log[type].Count > 0)
                return _log[type].Average(x => x.Value);
            return 0;
        }

        public double GetPeak(PerformanceType type)
        {
            var peak = _log[type]
                .GroupBy(x => x.SampleTime)
                .Select(x => new { Key = x.Key, MedianValue = x.Median(y => y.Value) })
                .OrderByDescending(x => x.MedianValue)
                .Select(x => x.MedianValue)
                .FirstOrDefault();
            return peak;
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
                _log.Clear();
                _log = null;
            }
        }

        public enum PerformanceType
        {
            MemoryUsed,
            CpuUsed,
            DiskTime,
            TestConcurrency,
            TestFixtureConcurrency,
            AssemblyConcurrency
        }

        public class PerformanceEntry
        {
            public DateTime SampleTime { get; set; } = DateTime.Now;
            public DateTime TimeSlot { get; set; }
            public double Value { get; set; }
            public PerformanceEntry(double value)
            {
                Value = value;
                TimeSlot = SampleTime.Subtract(new TimeSpan(0, 0, SampleTime.Second >= PerformanceLog.PeakSampleTimeSeconds ? PerformanceLog.PeakSampleTimeSeconds : 0));
            }
        }
    }

    
}
