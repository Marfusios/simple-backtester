using System;

namespace RangeBarProfit
{
    public class TradeModel
    {
        public double Timestamp { get; set; }

        public DateTime TimestampDate => DateUtils.ConvertToTime(Timestamp);

        public double Price { get; set; }
        public double Amount { get; set; }

        public int BarIndex { get; set; }
    }
}
