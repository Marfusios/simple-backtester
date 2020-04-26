using System;
using System.Diagnostics;
using CsvHelper.Configuration.Attributes;

namespace SimpleBacktester.Data
{
    [DebuggerDisplay("Bar {Index} {TimestampDate} {CurrentPrice}")]
    public class RangeBarModel
    {
        public double Timestamp { get; set; }

        public double? Mid { get; set; }

        public double? Bid { get; set; }
        public double? Ask { get; set; }

        public double? Open { get; set; }
        public double? High { get; set; }
        public double? Low { get; set; }
        public double? Close { get; set; }

        [Ignore] 
        public double CurrentPrice => Close ?? Mid ?? 0;

        [Ignore]
        public int Index { get; set; }

        [Ignore]
        public DateTime TimestampDate => DateUtils.ConvertToTimeFromSec(Timestamp);


        // TODO: make generic
        [Name("timestamp_diff_ms")]
        public double TimestampDiffMs { get; set; }
    }
}
