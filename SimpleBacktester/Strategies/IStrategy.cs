using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public enum Action
    {
        Nothing,
        Buy,
        Sell
    }

    public interface IStrategy
    {
        Action Decide(RangeBarModel bar, double inventoryAbsolute);
    }
}
