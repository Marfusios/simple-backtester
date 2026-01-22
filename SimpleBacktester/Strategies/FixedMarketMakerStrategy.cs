using System;
using System.Linq;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies;

public class FixedMarketMakerStrategy : IMakerStrategy
{
    private readonly int _slowdown;
    private readonly double _targetSpread;
    private readonly bool _reduceInventory;
    
    private int _waitingCounter;

    public FixedMarketMakerStrategy(double targetSpread, int slowdown, bool reduceInventory)
    {
        _targetSpread = targetSpread;
        _slowdown = slowdown;
        _reduceInventory = reduceInventory;
    }

    public PlacedOrder[] Decide(RangeBarModel bar, double inventoryAbsolute, int maxInventoryLimit, 
        PlacedOrder[] placedOrders, PlacedOrder[] executedOrders)
    {
        if (_waitingCounter > 0)
        {
            _waitingCounter -= 1;
            return Array.Empty<PlacedOrder>();
        }

        if (executedOrders.Any() && _slowdown > 0)
        {
            _waitingCounter = _slowdown-1;
            return Array.Empty<PlacedOrder>();
        }
        
        var midPrice = bar.Mid ?? bar.CurrentPrice;
        var targetSpreadAbs = midPrice * (_targetSpread / 100);
        
        var spreadBid = midPrice - targetSpreadAbs;
        var spreadAsk = midPrice + targetSpreadAbs;

        var amount = _reduceInventory ? Math.Max(Math.Abs(inventoryAbsolute), 1) : 1;
        
        return
        [
            new PlacedOrder(OrderSide.Bid, spreadBid, inventoryAbsolute < 0 ? amount : 1),
            new PlacedOrder(OrderSide.Ask, spreadAsk, inventoryAbsolute > 0 ? amount : 1)
        ];
    }
}