using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class MarketMakerStrategy : IMakerStrategy
    {
        private readonly int _skipBars;
        private int _barCounter = -1;

        public MarketMakerStrategy(int skipBars)
        {
            _skipBars = skipBars;
        }

        public PlacedOrder[] Decide(RangeBarModel bar, double inventoryAbsolute, PlacedOrder[] placedOrders)
        {
            _barCounter++;
            if (_barCounter % _skipBars != 0)
                return placedOrders;
            
            return new[]
            {
                new PlacedOrder(OrderSide.Bid, (bar.Bid ?? bar.CurrentPrice) - 100, inventoryAbsolute >= 0 ? 1 : inventoryAbsolute),
                new PlacedOrder(OrderSide.Ask, (bar.Ask ?? bar.CurrentPrice) + 100, inventoryAbsolute <= 0 ? 1 : inventoryAbsolute),
                //new PlacedOrder(OrderSide.Bid, (bar.Bid ?? bar.CurrentPrice) - 50, 1),
                //new PlacedOrder(OrderSide.Ask, (bar.Ask ?? bar.CurrentPrice) + 50, 1),
            };
        }
    }
}
