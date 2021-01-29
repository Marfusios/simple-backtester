namespace SimpleBacktester
{
    public class BacktestConfig
    {
        public string BaseSymbol { get; set; }
        public string QuoteSymbol { get; set; }
        public double? Amount { get; set; }
        public string DirectoryPath { get; set; }
        public string FilePattern { get; set; } = "*.csv";

        public string TimestampType { get; set; }
        public int? TimestampDecimals { get; set; }

        public double? FeePercentage { get; set; }
        public bool? DisplayFee { get; set; }
        public int[] MaxInventory { get; set; }

        public bool? Visualize { get; set; } = true;

        public int? VisualizeLimitBars { get; set; }
        public int? VisualizeSkipBars { get; set; }

        public int? LimitFiles { get; set; }
        public int? SkipFiles { get; set; }

        public bool? RunWebVisualization { get; set; }
    }
}
