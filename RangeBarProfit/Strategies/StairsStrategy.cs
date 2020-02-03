namespace RangeBarProfit.Strategies
{
    public class StairsStrategy : IStrategy
    {
        private readonly bool _preserverLastBar;
        
        private RangeBarModel _lastBar;

        public StairsStrategy(bool preserverLastBar)
        {
            _preserverLastBar = preserverLastBar;
        }

        public Action Decide(RangeBarModel bar, double inventory)
        {
            if (_lastBar == null)
            {
                _lastBar = bar;
                return Action.Nothing;
            }


            var open = _lastBar.Mid;
            var close = bar.Mid;

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
