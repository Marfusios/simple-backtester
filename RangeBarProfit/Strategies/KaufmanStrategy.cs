namespace RangeBarProfit.Strategies
{
    public class KaufmanStrategy : IStrategy
    {
        private readonly bool _largerPosition;

        private RangeBarModel _lastBar;

        private int _upScore;
        private int _downScore;

        private int _barsInTrade;

        public KaufmanStrategy(bool largerPosition)
        {
            _largerPosition = largerPosition;
        }

        public Action Decide(RangeBarModel bar, double inventory)
        {
            var oldBar = _lastBar;
            _lastBar = bar;

            if (oldBar == null)
            {
                // initial
                return Action.Nothing;
            }

            if (bar.Mid > oldBar.Mid)
            {
                _upScore += 1;
            }
            else
            {
                _upScore = 0;
            }

            if (bar.Mid < oldBar.Mid)
            {
                _downScore += 1;
            }
            else
            {
                _downScore = 0;
            }

            var hasLong = inventory > 0;

            if (hasLong)
            {
                _barsInTrade += 1;
            }

            if (hasLong && _barsInTrade >= 8)
            {
                // exit position as time stop loss
                _barsInTrade = 0;
                return Action.Sell;
            }

            if (_downScore >= 4 && (_largerPosition || !hasLong))
            {
                // open position
                _upScore = 0;
                _downScore = 0;
                return Action.Buy;
            }

            if (hasLong && _upScore >= 5)
            {
                // close position
                _upScore = 0;
                _downScore = 0;
                return Action.Sell;
            }

            return Action.Nothing;
        }
    }
}
