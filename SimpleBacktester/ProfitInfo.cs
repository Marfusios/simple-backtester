using System.Diagnostics;

namespace SimpleBacktester
{
    [DebuggerDisplay("Report [{Day}/{Month}/{Year}] t: {TradesCount} pnl: {Pnl.ToString(\"0.00\")} ({PnlNoExcess.ToString(\"0.00\")}) avg: {AverageBuyPrice.ToString(\"0.00\")}/{AverageSellPrice.ToString(\"0.00\")}")]
    public class ProfitInfo
    {

        public int TradesCount { get; set; }
        public int BuysCount { get; set; }
        public int SellsCount { get; set; }

        public double TotalBought { get; set; }
        public double TotalSold { get; set; }

        public double TotalBoughtQuote { get; set; }
        public double TotalSoldQuote { get; set; }

        public double AverageBuyPrice { get; set; }
        public double AverageSellPrice { get; set; }

        public string QuoteSymbol { get; set; }
        public string BaseSymbol { get; set; }

        public int CurrentInventory { get; set; }
        public double ExcessAmount { get; set; }

        public double OrderSize { get; set; }

        public int MaxInventory { get; set; }
        public int? MaxInventoryLimit { get; set; }


        public double Pnl { get; set; }
        public double PnlNoExcess { get; set; }
        public double PnlWithFee { get; set; }

        public bool DisplayWithFee { get; set; }

        public string Report { get; set; }

        public int? Year { get; set; }

        public int? Month { get; set; }

        public int? Day { get; set; }

        public double WinRate { get; set; }

        public double? MaxDrawdownPercentage { get; set; }
        public double? ProfitPercentage { get; set; }

        public ProfitInfo[] SubProfits { get; set; } = new ProfitInfo[0];

        public override string ToString()
        {
            var feeString = DisplayWithFee ? $"(with fee: {PnlWithFee:#.00} {QuoteSymbol})" : string.Empty;
            var profitString = ProfitPercentage.HasValue ? $" ({ProfitPercentage*100:F}%)" : string.Empty;

            return $"trades {TradesCount,5} " +
                   $"(b: {BuysCount,5}/{AverageBuyPrice,8:#.00} {QuoteSymbol}, s: {SellsCount,5}/{AverageSellPrice,8:#.00} {QuoteSymbol}), " +
                   $"Win: {(WinRate*100),7:#.00}%, " +
                   $"MDD: {(MaxDrawdownPercentage.HasValue ? DisplayMaxDD(MaxDrawdownPercentage.Value) : "       ")}, " +
                   $"Pnl: {Pnl,10:#.00} {QuoteSymbol}{profitString} {feeString}";
        }

        private string DisplayMaxDD(double maxDrawdownPercentage)
        {
            return $"{maxDrawdownPercentage*100,7:#.00}%";
        }

        public ProfitInfo Clone()
        {
            return new ProfitInfo()
            {
                TradesCount = TradesCount,
                AverageBuyPrice = AverageBuyPrice,
                AverageSellPrice = AverageSellPrice,
                Year = Year,
                Month = Month,
                Day = Day,
                Report = Report,
                BaseSymbol = BaseSymbol,
                BuysCount = BuysCount,
                CurrentInventory = CurrentInventory,
                DisplayWithFee = DisplayWithFee,
                MaxInventory = MaxInventory,
                MaxInventoryLimit = MaxInventoryLimit,
                OrderSize = OrderSize,
                Pnl = Pnl,
                PnlNoExcess = PnlNoExcess,
                PnlWithFee = PnlWithFee,
                QuoteSymbol = QuoteSymbol,
                SellsCount = SellsCount,
                ExcessAmount = ExcessAmount,
                TotalBought = TotalBought,
                TotalSold = TotalSold,
                TotalBoughtQuote = TotalBoughtQuote,
                TotalSoldQuote = TotalSoldQuote,
                WinRate = WinRate
            };
        }

        public static readonly ProfitInfo Empty = new ProfitInfo();
    }
}
