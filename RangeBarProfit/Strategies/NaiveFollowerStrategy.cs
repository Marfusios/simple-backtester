﻿namespace RangeBarProfit.Strategies
{
    public class NaiveFollowerStrategy : IStrategy
    {
        private readonly bool _againstTrend;
        private RangeBarModel _lastBar;

        public NaiveFollowerStrategy(bool againstTrend)
        {
            _againstTrend = againstTrend;
        }

        public Action Decide(RangeBarModel bar, double inventory)
        {
            if (_lastBar == null)
            {
                _lastBar = bar;
                return Action.Nothing;
            }

            var lastUp = _lastBar.MidChange >= 0;
            _lastBar = bar;

            if(_againstTrend)
                return lastUp ? Action.Sell : Action.Buy;
            return lastUp ? Action.Buy : Action.Sell;
        }
    }
}