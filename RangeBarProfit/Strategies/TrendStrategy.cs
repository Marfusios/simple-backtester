using System;

namespace RangeBarProfit.Strategies
{
    public class TrendStrategy : IStrategy
    {
        private readonly bool _aggressive;
        private int _trendCounter;

        public TrendStrategy(bool aggressive)
        {
            _aggressive = aggressive;
        }

        public Action Decide(RangeBarModel bar)
        {
            if(_aggressive)
                return DecideAggressive(bar);
            return DecideConservative(bar);
        }

        private Action DecideAggressive(RangeBarModel bar)
        {
            var currentTrend = Math.Sign(bar.MidChange);
            var sameTrend = Math.Sign(_trendCounter) == currentTrend;

            if (Math.Abs(_trendCounter) >= 2)
            {
                // trend, check if current is opposite
                if (!sameTrend)
                {
                    // opposite trend, close position
                    _trendCounter = currentTrend;
                    return currentTrend < 0 ? Action.Sell : Action.Buy;
                }
            }

            if (sameTrend)
                _trendCounter += currentTrend;
            else
                _trendCounter += 2 * currentTrend;
            return Action.Nothing;
        }

        private Action DecideConservative(RangeBarModel bar)
        {
            var currentTrend = Math.Sign(bar.MidChange);

            if (Math.Abs(_trendCounter) >= 2)
            {
                // trend, check if current is opposite
                var sameTrend = Math.Sign(_trendCounter) == currentTrend;
                if (!sameTrend)
                {
                    // opposite trend, close position
                    _trendCounter = 0;
                    return currentTrend < 0 ? Action.Sell : Action.Buy;
                }
            }

            _trendCounter += currentTrend;
            return Action.Nothing;
        }
    }
}
