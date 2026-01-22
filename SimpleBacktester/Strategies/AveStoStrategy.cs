using System;
using System.Linq;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    /// <summary>
    /// Avellaneda-Stoikov market making model
    /// </summary>
    public class AveStoStrategy : IMakerStrategy
    {
        private readonly int _slowdown;
        private readonly double _riskFactor;

        private double _lastInventory = 0;
        private int _waitingCounter;
        
        public AveStoStrategy(int slowdown, double riskFactor)
        {
            _riskFactor = riskFactor;
            _slowdown = slowdown;
        }

        public PlacedOrder[] Decide(RangeBarModel bar, double inventoryAbsolute, int maxInventoryLimit, 
            PlacedOrder[] placedOrders, PlacedOrder[] executedOrders)
        {
            if (_waitingCounter > 0)
            {
                _waitingCounter -= 1;
                return placedOrders;
            }

            if (executedOrders.Any())
            {
                _waitingCounter = _slowdown;
                return placedOrders;
            }
            
            var midPrice = bar.Mid ?? bar.CurrentPrice;

            var inventory = inventoryAbsolute / maxInventoryLimit;
            var risk = _riskFactor / 10;
            var volatility = 10.0;
            var orderBookLiquidity = 0.5;

            var reservationPrice = midPrice - (inventory * risk * volatility);
            var spread = 2 * risk * volatility + (2 / risk) * Math.Log(1 + risk / orderBookLiquidity);

            var spreadBid = reservationPrice - spread / 2;
            var spreadAsk = reservationPrice + spread / 2;

            return
            [
                new PlacedOrder(OrderSide.Bid, spreadBid, 1),
                new PlacedOrder(OrderSide.Ask, spreadAsk, 1)
            ];
        }
    }
}