using System;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class TradeFlowStrategy : ITakerStrategy
    {
        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            var tradeFlow = ComputeTradeFlow(bar);

            if (Math.Abs(tradeFlow) <= 0.33)
            {
                // weak signal, do nothing
                return Action.Nothing;
            }

            return tradeFlow >= 0 ? Action.Buy : Action.Sell;
        }

        private double ComputeTradeFlow(RangeBarModel bar)
        {
            var buys = bar.BuyVolume ?? 0;
            var sells = bar.SellVolume ?? 0;

            if (buys + sells == 0)
                return 0;

            return (buys - sells) / (buys + sells);
        }
    }
}
