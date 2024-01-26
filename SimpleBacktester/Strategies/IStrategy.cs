using System;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public enum Action
    {
        Nothing,
        Buy,
        Sell
    }

    public enum OrderSide
    {
        Bid,
        Ask
    }

    public class PlacedOrder
    {
        public PlacedOrder(OrderSide side, double price, double? amount)
        {
            Side = side;
            Price = Math.Abs(price);
            Amount = amount == null ? (double?)null : Math.Abs(amount.Value);
        }

        public OrderSide Side { get; }
        public double Price { get; }
        public double? Amount { get; }
    }

    public interface IStrategy
    {

    }

    public interface ITakerStrategy : IStrategy
    {
        Action Decide(RangeBarModel bar, double inventoryAbsolute);
    }

    public interface IMakerStrategy : IStrategy
    {
        PlacedOrder[] Decide(RangeBarModel bar, double inventoryAbsolute, PlacedOrder[] placedOrders);
    }
}
