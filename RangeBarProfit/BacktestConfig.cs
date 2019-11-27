namespace RangeBarProfit
{
    public class BacktestConfig
    {
        public string BaseSymbol { get; set; }
        public string QuoteSymbol { get; set; }
        public double Amount { get; set; }
        public string DirectoryPath { get; set; }

        public double FeePercentage { get; set; } = 0.00075;
        public int? MaxInventory { get; set; } = 2;

        public bool Visualize { get; set; } = true;
        public bool VisualizeByTime { get; set; } = false;
    }
}
