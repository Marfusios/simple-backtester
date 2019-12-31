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

        public string Report { get; set; }

        public int? Month { get; set; }

        public int? Day { get; set; }

        public override string ToString()
        {
            var feeString = DisplayWithFee ? $"(with fee: {PnlWithFee:#.00} {QuoteSymbol})" : string.Empty;
            return $"trades {TradesCount,5} " +
                   $"(b: {BuysCount,5}/{AverageBuy,8:#.00} {QuoteSymbol}, s: {SellsCount,5}/{AverageSell,8:#.00} {QuoteSymbol}), " +
                   $"Inv: {CurrentInventory * OrderSize} {BaseSymbol} (max: {MaxInventory * OrderSize}/{MaxInventoryLimit} {BaseSymbol}), " +
                   $"Pnl: {Pnl,10:#.00} {QuoteSymbol} {feeString}";
        }

        public ProfitInfo Clone()
        {
            return new ProfitInfo()
            {
                TradesCount = TradesCount,
                AverageBuy = AverageBuy,
                AverageSell = AverageSell,
                Month = Month,
                Report = Report,
                Day = Day,
                BaseSymbol = BaseSymbol,
                BuysCount = BuysCount,
                CurrentInventory = CurrentInventory,
                DisplayWithFee = DisplayWithFee,
                MaxInventory = MaxInventory,
                MaxInventoryLimit = MaxInventoryLimit,
                OrderSize = OrderSize,
                Pnl = Pnl,
                PnlWithFee = PnlWithFee,
                QuoteSymbol = QuoteSymbol,
                SellsCount = SellsCount
            };
        }
    }
}
