using System;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class MeanReversionStrategy : IStrategy
    {
        private readonly int _trendConfirmationCount;

        private RangeBarModel _previousBar;
        private bool _isUpTrend;
        private int _currentConfirmation;

        public MeanReversionStrategy(int trendConfirmationCount)
        {
            _trendConfirmationCount = Math.Max(trendConfirmationCount, 1);
        }

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            var previous = _previousBar;
            _previousBar = bar;

            if (previous == null)
            {
                // initial state
                return Action.Nothing;
            }

            if (bar.CurrentPrice > previous.CurrentPrice && !_isUpTrend && _currentConfirmation >= _trendConfirmationCount)
            {
                _isUpTrend = true;
                _currentConfirmation = 0;
                return Action.Buy;
            }

            if (bar.CurrentPrice < previous.CurrentPrice && _isUpTrend && _currentConfirmation >= _trendConfirmationCount)
            {
                _isUpTrend = false;
                _currentConfirmation = 0;
                return Action.Sell;
            }

            _currentConfirmation += 1;
            return Action.Nothing;
        }
    }
}
