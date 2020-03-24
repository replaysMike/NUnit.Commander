using AnyConsole;
using NUnit.Commander.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NUnit.Commander.Display
{
    public class AsciiChart
    {
        public enum AsciiChartStyle
        {
            Gradient,
            InvertedGradient,
            Block,
            InvertedBlock
        }

        public AsciiChartStyle ChartStyle { get; set; } = AsciiChartStyle.Gradient;
        public int Width { get; set; }
        public int Height { get; set; }
        public AsciiChart(int width, int height, AsciiChartStyle ChartStyle = AsciiChartStyle.Gradient)
        {
            Width = width;
            Height = height;
        }

        public string GraphXY(Dictionary<DateTime, double> data)
        {
            return GraphXY(data, Color.Gray, Color.Gray).ToString();
        }

        public ColorTextBuilder GraphXY(Dictionary<DateTime, double> data, Color foregroundColor, Color backgroundColor)
        {
            var chart = new ColorTextBuilder();
            char emptyValueChar;
            char valueChar;
            switch (ChartStyle)
            {
                case AsciiChartStyle.Block:
                    emptyValueChar = '\u2581'; // ▁
                    valueChar = '\u2588'; // █ 
                    break;
                case AsciiChartStyle.InvertedBlock:
                    emptyValueChar = '\u2587'; // █ 
                    valueChar = '\u2581'; // ▁
                    break;
                case AsciiChartStyle.Gradient:
                default:
                    emptyValueChar = '\u2591'; // ░
                    valueChar = '\u2593'; // ▓
                    break;
                case AsciiChartStyle.InvertedGradient:
                    emptyValueChar = '\u2593'; // ▓
                    valueChar = '\u2591'; // ░
                    break;
            }

            // sort the data into time slices to fit the X access on the chart
            var slicedData = GetXYSlices(data, Width, Height);
            for (var y = Height; y >= 0; y--)
            {
                // for each x value (time), print a character for the y axis
                for(var x = 0; x < slicedData.Count; x++)
                {
                    var val = slicedData.Skip(x).Select(z => z.Value).FirstOrDefault();
                    if (val >= y)
                        chart.Append(valueChar.ToString(), foregroundColor);
                    else
                        chart.Append(emptyValueChar.ToString(), backgroundColor);
                }
                chart.AppendLine();
            }

            return chart;
        }

        private Dictionary<DateTime, double> GetXYSlices(Dictionary<DateTime, double> data, int width, int height)
        {
            var slicedData = new Dictionary<DateTime, double>();
            var orderedKeys = data.Keys.OrderBy(x => x.Ticks);
            var orderedValues = data.Values.OrderBy(x => x);
            var minXValue = orderedKeys.FirstOrDefault();
            var maxXValue = orderedKeys.LastOrDefault();
            var minYValue = orderedValues.FirstOrDefault();
            var maxYValue = orderedValues.LastOrDefault();

            var xDivision = maxXValue.Subtract(minXValue).TotalSeconds / width;
            var yDivision = maxYValue - minYValue;
            for (var i = 0; i < width; i++)
            {
                // get the average of this sample size
                var segmentStart = minXValue.AddSeconds(i * xDivision);
                var segmentEnd = segmentStart.AddSeconds(xDivision);
                var samples = data.Where(x => x.Key >= segmentStart && x.Key < segmentEnd).ToList();
                var avg = 0.0;
                if (samples.Any())
                {
                    avg = samples.Average(x => x.Value);
                }
                slicedData.Add(segmentStart, avg);
            }
            // compute the scale based on final values
            var yScale = height / (slicedData.Max(x => x.Value) - slicedData.Min(x => x.Value));
            foreach (var key in slicedData.Keys.ToList())
                slicedData[key] *= yScale;

            return slicedData;
        }
    }
}
