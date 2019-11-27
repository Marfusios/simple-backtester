using System;
using System.IO;
using System.Linq;
using CsvHelper;
using RangeBarProfit.Strategies;

namespace RangeBarProfit
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Range bars profit computer");

            var baseDir = "C:\\dev\\work\\manana\\data\\bitmex\\price-range-bars\\2019-11";

            var backtests = new[]
            {
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = Path.Combine(baseDir, "xbtusd-002"),
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = Path.Combine(baseDir, "xbtusd-001"),
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = Path.Combine(baseDir, "xbtusd-0005"),
                    Visualize = false
                },
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = Path.Combine(baseDir, "xbth20-002"),
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = Path.Combine(baseDir, "xbth20-001"),
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = Path.Combine(baseDir, "xbth20-0005"),
                    Visualize = false
                },
                new BacktestConfig
                {
                    BaseSymbol = "ETH",
                    QuoteSymbol = "USD",
                    Amount = 10,
                    DirectoryPath = Path.Combine(baseDir, "ethusd-002"),
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "ETH",
                    QuoteSymbol = "USD",
                    Amount = 10,
                    DirectoryPath = Path.Combine(baseDir, "ethusd-001"),
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "ETH",
                    QuoteSymbol = "USD",
                    Amount = 10,
                    DirectoryPath = Path.Combine(baseDir, "ethusd-0005"),
                    Visualize = false
                },
            };



            foreach (var backtest in backtests)
            {
                //var strategy = new NaiveStrategy();
                var strategy = new TrendStrategy(false);

                var computer = RunBacktest(backtest, strategy);
                Visualize(backtest, computer, strategy);
            }
        }

        private static ProfitComputer RunBacktest(BacktestConfig backtest, IStrategy strategy)
        {
            var files = LoadAllFiles(backtest.DirectoryPath);//.Take(1).ToArray();
            var computer = new ProfitComputer(
                backtest.BaseSymbol, 
                backtest.QuoteSymbol, 
                backtest.Amount, 
                strategy,
                backtest.FeePercentage,
                backtest.MaxInventory
                );

            Console.WriteLine();
            Console.WriteLine("=====================================================================");
            Console.WriteLine($"    Running for {files.Length} files from dir '{backtest.DirectoryPath}'");

            RangeBarModel lastBar = null;
            foreach (var file in files)
            {
                var bars = LoadBars(file);
                computer.ProcessBars(bars);
                lastBar = bars.LastOrDefault();
            }

            if (lastBar != null)
                computer.ProcessLastBar(lastBar);

            Console.WriteLine($"    {computer.GetReport()}");
            //Console.WriteLine("=====================================================================");
            Console.WriteLine();
            return computer;
        }

        private static RangeBarModel[] LoadBars(string file)
        {
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader);
            csv.Configuration.PrepareHeaderForMatch = (header, index) => header.ToLower();
            return csv.GetRecords<RangeBarModel>().ToArray();
        }

        private static string[] LoadAllFiles(string dirPath)
        {
            var files = Directory.EnumerateFiles(dirPath, "*.csv");
            return files.OrderBy(x => x).ToArray();
        }

        private static void Visualize(BacktestConfig backtest, ProfitComputer computer, IStrategy strategy)
        {
            if (!backtest.Visualize)
                return;

            var chart = new ChartVisualizer();
            var filename = Path.GetFileName(backtest.DirectoryPath);
            var strategyName = strategy.GetType().Name;
            var pnl = computer.GetPnl();
            var name = $"{filename} {strategyName} pnl: {pnl:#.00} {backtest.QuoteSymbol}";
            var targetFile = Path.Combine(Path.GetDirectoryName(backtest.DirectoryPath), "reports", $"{filename}__{strategyName}");
            //if (!Directory.Exists(targetFile))
            //    Directory.CreateDirectory(targetFile);

            chart.PlotTrades(name, targetFile, computer.Bars, computer.Trades, backtest.VisualizeByTime);
        }
    }
}
