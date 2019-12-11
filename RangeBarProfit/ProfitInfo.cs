namespace RangeBarProfit
{
    public class ProfitInfo
    {
        public int TradesCount { get; set; }
        public int BuysCount { get; set; }
        public int SellsCount { get; set; }

        public double AverageBuy { get; set; }
        public double AverageSell { get; set; }

        public string QuoteSymbol { get; set; }
        public string BaseSymbol { get; set; }

        public int CurrentInventory { get; set; }

        public double OrderSize { get; set; }

        public int MaxInventory { get; set; }
        public int? MaxInventoryLimit { get; set; }


        public double Pnl { get; set; }
        public double PnlWithFee { get; set; }

        public bool DisplayWithFee { get; set; }

        public override string ToString()
        {
            var feeString = DisplayWithFee ? $"(with fee: {PnlWithFee:#.00} {QuoteSymbol})" : string.Empty;
            return $"trades {TradesCount} " +
                   $"(b: {BuysCount}/{AverageBuy:#.00} {QuoteSymbol}, s: {SellsCount}/{AverageSell:#.00} {QuoteSymbol}), " +
                   $"Inv: {CurrentInventory * OrderSize} {BaseSymbol} (max: {MaxInventory * OrderSize}/{MaxInventoryLimit} {BaseSymbol}), " +
                   $"Pnl: {Pnl:#.00} {QuoteSymbol} {feeString}";
        }
    }
}
