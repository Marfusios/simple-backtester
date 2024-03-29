﻿using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class NaiveStrategy : ITakerStrategy
    {
        private bool _lastSell = true;

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            if (_lastSell)
            {
                _lastSell = false;
                return Action.Buy;
            }
            if (!_lastSell)
            {
                _lastSell = true;
                return Action.Sell;
            }

            return Action.Nothing;
        }
    }
}
