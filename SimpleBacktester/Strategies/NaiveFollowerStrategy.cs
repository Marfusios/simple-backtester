using System;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class NaiveFollowerStrategy : ITakerStrategy
    {
        private readonly bool _againstTrend;
        private RangeBarModel _lastBar;
        private double _lastMidChange;

        public NaiveFollowerStrategy(bool againstTrend)
        {
            _againstTrend = againstTrend;
        }

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            if (_lastBar == null)
            {
                _lastBar = bar;
                return Action.Nothing;
            }

            //var previousMidChange = _lastMidChange;
            _lastMidChange = bar.CurrentPrice - _lastBar.CurrentPrice;

            //if (Math.Abs(_lastMidChange) < 1)
            //{
            //    return Action.Nothing;
            //}

            var lastUp = _lastMidChange >= 0;
            _lastBar = bar;

            if (_againstTrend)
                return lastUp ? Action.Sell : Action.Buy;
            return lastUp ? Action.Buy : Action.Sell;
        }
    }
}
