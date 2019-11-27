namespace RangeBarProfit.Strategies
{
    public class NaiveStrategy : IStrategy
    {
        private bool _lastSell = true;

        public Action Decide(RangeBarModel bar)
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
