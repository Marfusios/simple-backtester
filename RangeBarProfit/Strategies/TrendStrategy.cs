using System;

namespace RangeBarProfit.Strategies
{
    public class TrendStrategy : IStrategy
    {
        private readonly bool _aggressive;
        private int _trendCounter;

        //private int _lastMonth = -1;

        public TrendStrategy(bool aggressive)
        {
            _aggressive = aggressive;
        }

        public Action Decide(RangeBarModel bar, double inventory)
        {
            //var currentMonth = bar.TimestampDate.Month;
            //if (_lastMonth < 0)
            //    _lastMonth = currentMonth;

            //if (_lastMonth != currentMonth)
            //{
            //    // month changed, reduce inventory
            //    _lastMonth = currentMonth;
                
            //    if(Math.Abs(inventory) > 0)
            //        return inventory >= 0 ? Action.Sell : Action.Buy;
            //}

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
