using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class OrderBookStrategy : ITakerStrategy
    {
        private double? _lastRatio;

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            var liquidityRatio = (bar.ObLiquidityBid - bar.ObLiquidityAsk) / (bar.ObLiquidityBid + bar.ObLiquidityAsk);
            if (_lastRatio == null)
            {
                _lastRatio = liquidityRatio;
                return Action.Nothing;
            }
            var ratioDiff = liquidityRatio - _lastRatio;
            var ratioDiffPer = ratioDiff * 100;

            _lastRatio = liquidityRatio;

            if (ratioDiffPer > 4)
                return Action.Sell;
            if (ratioDiffPer < -4)
                return Action.Buy;

            return Action.Nothing;
        }
    }
}
