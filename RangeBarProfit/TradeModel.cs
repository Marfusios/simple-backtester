using System;
using System.Diagnostics;

namespace RangeBarProfit
{
    [DebuggerDisplay("Trade {BarIndex} {TimestampDate} {Amount} @ {Price} inv: {CurrentInventory} {PositionState}")]
    public class TradeModel
    {
        public double Timestamp { get; set; }

        public DateTime TimestampDate => DateUtils.ConvertToTime(Timestamp);

        public double Price { get; set; }
        public double Amount { get; set; }

        public int BarIndex { get; set; }

        public double CurrentInventory { get; set; }

        public PositionState PositionState { get; set; }
    }
}
