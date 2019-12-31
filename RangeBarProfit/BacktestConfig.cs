namespace RangeBarProfit
{
    public class BacktestConfig
    {
        public string BaseSymbol { get; set; }
        public string QuoteSymbol { get; set; }
        public double Amount { get; set; }
        public string DirectoryPath { get; set; }
        public string FilePattern { get; set; } = "*.csv";
        public string Range { get; set; }

        public double FeePercentage { get; set; } = 0.00075;
        public bool DisplayFee { get; set; } = true;
        public int[] MaxInventory { get; set; } = { 1, 2, 4, 20 };

        public bool Visualize { get; set; } = true;
        public bool VisualizeByTime { get; set; } = false;

        public int? VisualizeLimitBars { get; set; } = 200;
        public int? VisualizeSkipBars { get; set; } = null;

        public int? LimitFiles { get; set; } = null;
        public int? SkipFiles { get; set; } = null;
    }
}
