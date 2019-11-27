namespace RangeBarProfit.Strategies
{
    public enum Action
    {
        Nothing,
        Buy,
        Sell
    }

    public interface IStrategy
    {
        Action Decide(RangeBarModel bar);
    }
}
