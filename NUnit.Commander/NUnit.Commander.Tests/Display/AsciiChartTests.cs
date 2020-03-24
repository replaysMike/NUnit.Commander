using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Commander.Display;
using System.Linq;

namespace NUnit.Commander.Tests.Display
{
    [TestFixture]
    public class AsciiChartTests
    {
        [Test]
        public void Should_GraphXY()
        {
            var width = 20;
            var height = 18;
            var startTime = DateTime.Now;
            var data = new Dictionary<DateTime, double>()
            {
                { startTime.Add(TimeSpan.FromSeconds(0)), 0 },
                { startTime.Add(TimeSpan.FromSeconds(1)), 1 },
                { startTime.Add(TimeSpan.FromSeconds(2)), 3 },
                { startTime.Add(TimeSpan.FromSeconds(3)), 13 },
                { startTime.Add(TimeSpan.FromSeconds(4)), 18 },
                { startTime.Add(TimeSpan.FromSeconds(5)), 15 },
                { startTime.Add(TimeSpan.FromSeconds(6)), 15 },
                { startTime.Add(TimeSpan.FromSeconds(7)), 15 },
                { startTime.Add(TimeSpan.FromSeconds(8)), 7 },
                { startTime.Add(TimeSpan.FromSeconds(9)), 2 },

                { startTime.Add(TimeSpan.FromSeconds(10)), 0 },
                { startTime.Add(TimeSpan.FromSeconds(11)), 1 },
                { startTime.Add(TimeSpan.FromSeconds(12)), 3 },
                { startTime.Add(TimeSpan.FromSeconds(13)), 13 },
                { startTime.Add(TimeSpan.FromSeconds(14)), 18 },
                { startTime.Add(TimeSpan.FromSeconds(15)), 15 },
                { startTime.Add(TimeSpan.FromSeconds(16)), 15 },
                { startTime.Add(TimeSpan.FromSeconds(17)), 15 },
                { startTime.Add(TimeSpan.FromSeconds(18)), 7 },
                { startTime.Add(TimeSpan.FromSeconds(19)), 2 },
            };
            var chart = new AsciiChart(width, height);
            var result = chart.GraphXY(data);

            Assert.NotNull(result);
            Assert.AreEqual(width, result.IndexOf('\n') - 1);
            Assert.AreEqual(height, result.Count(c => c == '\n' ) - 1);
        }
    }
}
