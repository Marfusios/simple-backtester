﻿using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class StairsStrategy : ITakerStrategy
    {
        private readonly bool _preserverLastBar;
        
        private RangeBarModel _lastBar;

        public StairsStrategy(bool preserverLastBar)
        {
            _preserverLastBar = preserverLastBar;
        }

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            if (_lastBar == null)
            {
                _lastBar = bar;
                return Action.Nothing;
            }


            var open = _lastBar.CurrentPrice;
            var close = bar.CurrentPrice;

            //var open = bar.Open;
            //var close = bar.Close;

            if (!_preserverLastBar)
            {
                _lastBar = bar;
            }

            if (close > open)
            {
                // green bar
                return Action.Sell;
            }

            if (close < open)
            {
                // red bar
                return Action.Buy;
            }


            _lastBar = bar;
            return Action.Nothing;
        }
    }
}
