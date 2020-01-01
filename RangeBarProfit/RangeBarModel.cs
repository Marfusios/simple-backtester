using System;
using System.Diagnostics;
using CsvHelper.Configuration.Attributes;

namespace RangeBarProfit
{
    [DebuggerDisplay("Bar {Index} {TimestampDate} {Mid}")]
    public class RangeBarModel
    {
        [Ignore]
        public int Index { get; set; }
        public double Timestamp { get; set; }

        [Ignore]
        public DateTime TimestampDate => DateUtils.ConvertToTime(Timestamp);

        public double Mid { get; set; }

        public double Bid { get; set; }
        public double Ask { get; set; }

    }
}
