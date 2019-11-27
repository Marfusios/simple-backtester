using System;
using System.Collections.Generic;
using System.Text;
using CsvHelper.Configuration.Attributes;

namespace RangeBarProfit
{
    public class RangeBarModel
    {

        public double Timestamp { get; set; }

        [Name("timestamp_diff_ms")]
        public double TimeDiffMs { get; set; }

        public double Mid { get; set; }

        [Name("mid_change")]
        public double MidChange { get; set; }

        [Name("mid_change_agg")]
        public double MidChangeAgg { get; set; }

        public double Bid { get; set; }
        public double Ask { get; set; }

    }
}
