using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CsvHelper;
using SimpleBacktester.Data;
using SimpleBacktester.Strategies;

namespace SimpleBacktester
{
    class Program
    {
        static void Main(string[] args)
        {
            var env = args?.FirstOrDefault();
            var envText = string.IsNullOrWhiteSpace(env) ? string.Empty : $"env: {env}";

            Console.WriteLine($@"
    ██████   █████   ██████ ██   ██ ████████ ███████ ███████ ████████ ███████ ██████  
    ██   ██ ██   ██ ██      ██  ██     ██    ██      ██         ██    ██      ██   ██ 
    ██████  ███████ ██      █████      ██    █████   ███████    ██    █████   ██████  
    ██   ██ ██   ██ ██      ██  ██     ██    ██           ██    ██    ██      ██   ██ 
    ██████  ██   ██  ██████ ██   ██    ██    ███████ ███████    ██    ███████ ██   ██ 
                                                                                  
{envText}                                                                                  
");

            var config = InitConfig(env);
            MergeBacktestsWithBase(config);

            var strategyFactory = new Func<IStrategy>(() => ResolveStrategy(config.Strategy, config.StrategyParams));
            var strategy = strategyFactory();

            var paramsText = config.StrategyParams != null
                ? $"params: [{string.Join(", ", config.StrategyParams)}]"
                : string.Empty;
            Console.WriteLine($"[STRATEGY] '{strategy.GetType().Name}'  {paramsText}");

            foreach (var backtest in config.Backtests)
            {
                RunBacktest(backtest, strategyFactory);
            }
        }

        private static SimpleBacktesterConfig InitConfig(string environment)
        {
            var configRoot = ConfigUtils.InitConfig(environment);
            var config = new SimpleBacktesterConfig();
            ConfigUtils.FillConfig(configRoot, "config", config);
            return config;
        }

        private static void MergeBacktestsWithBase(SimpleBacktesterConfig config)
        {
            if(config.Backtests == null || !config.Backtests.Any())
                throw new Exception("Please configure at least one backtest in appsettings.json");

            if (config.Base == null)
                return;

            foreach (var backtest in config.Backtests)
            {
                backtest.BaseSymbol ??= config.Base.BaseSymbol;
                backtest.QuoteSymbol ??= config.Base.QuoteSymbol;
                backtest.Amount ??= config.Base.Amount;
                backtest.DirectoryPath ??= config.Base.DirectoryPath;
                backtest.FilePattern ??= config.Base.FilePattern;

                backtest.TimestampType ??= config.Base.TimestampType;
                backtest.TimestampDecimals ??= config.Base.TimestampDecimals;

                backtest.FeePercentage ??= config.Base.FeePercentage;
                backtest.DisplayFee ??= config.Base.DisplayFee;

                backtest.MaxInventory ??= config.Base.MaxInventory;

                backtest.Visualize ??= config.Base.Visualize;
                backtest.VisualizeLimitBars ??= config.Base.VisualizeLimitBars;
                backtest.VisualizeSkipBars ??= config.Base.VisualizeSkipBars;

                backtest.SkipFiles ??= config.Base.SkipFiles;
                backtest.LimitFiles ??= config.Base.LimitFiles;
            }
        }

        private static IStrategy ResolveStrategy(string strategyName, object[] strategyParams)
        {
            if (string.IsNullOrWhiteSpace(strategyName))
                throw new Exception("Please configure strategy name in appsettings.json");

            var strategies = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IStrategy)))
                .ToArray();
            var strategy =
                strategies.FirstOrDefault(x => x.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
            if(strategy == null)
                throw new Exception($"There is no strategy with name '{strategyName}'");
            var parameters = FixParams(strategyParams);
            return (IStrategy)Activator.CreateInstance(strategy, parameters);
        }

        private static object[] FixParams(object[] strategyParams)
        {
            return strategyParams?.Select(x =>
                {
                    if (x is string xStr)
                    {
                        if (int.TryParse(xStr, out var xInt)) return xInt;
                        if (long.TryParse(xStr, out var xLong)) return xLong;
                        if (double.TryParse(xStr, out var xDouble)) return xDouble;
                        if (DateTime.TryParse(xStr, out var xDateTime)) return xDateTime;
                        if (bool.TryParse(xStr, out var xBool)) return xBool;
                    }

                    return x;
                })
                .ToArray();
        }


        private static void RunBacktest(BacktestConfig backtest, Func<IStrategy> strategyFactory)
        {
            var files = LoadAllFiles(backtest.DirectoryPath, backtest.FilePattern);
            if (backtest.SkipFiles.HasValue)
                files = files.Skip(backtest.SkipFiles.Value).ToArray();
            if (backtest.LimitFiles.HasValue)
                files = files.Take(backtest.LimitFiles.Value).ToArray();

            if (!files.Any())
                return;

            Console.WriteLine();
            Console.WriteLine("=====================================================================");
            Console.WriteLine($"    Running for {files.Length} files from dir '{backtest.DirectoryPath}' and pattern: '{backtest.FilePattern}'");

            var builderTop = new StringBuilder();
            var builder = new StringBuilder();

            foreach (var maxInventory in backtest.MaxInventory)
            {
                RangeBarModel lastBar = null;
                var strategy = strategyFactory();
                var computer = new ProfitComputer(strategy, backtest, maxInventory);
                foreach (var file in files)
                {
                    var bars = LoadBars(backtest, file);
                    computer.ProcessBars(bars);
                    lastBar = bars.LastOrDefault();
                }

                if (lastBar != null)
                    computer.ProcessLastBar(lastBar);

                var report = computer.GetReport();

                builderTop.AppendLine(report.ToString());

                builder.AppendLine(
                    $"==== MAX INV: {maxInventory} {new string('=', 133)}");
                builder.AppendLine();
                builder.AppendLine(report.ToString());
                Console.WriteLine($"    {report}");

                var reportDays = new List<ProfitInfo>();
                var perMonth = computer.GetReportByMonth();
                foreach (var month in perMonth)
                {
                    builder.AppendLine($"    {month.Report}");
                    if (month.Month == null)
                    {
                        builderTop.AppendLine($"{month.Report}");
                        continue;
                    }
                    var perDay = computer.GetReportPerDays(month.Year.Value, month.Month.Value);
                    reportDays.AddRange(perDay.Where(x => x.Day != null));
                    foreach (var day in perDay)
                    {
                        builder.AppendLine($"        {day.Report}");
                    }
                }

                builderTop.AppendLine();
                builder.AppendLine();
                builder.AppendLine();

                Visualize(backtest, computer, strategy, maxInventory, report, reportDays.ToArray());
            }

            var mergedReport = $"{builderTop}{Environment.NewLine}{builder}";
            SaveTextReport(mergedReport, backtest, strategyFactory());

            Console.WriteLine();
        }

        private static RangeBarModel[] LoadBars(BacktestConfig config, string file)
        {
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Configuration.PrepareHeaderForMatch = (header, index) => header.ToLower();
            csv.Configuration.HeaderValidated = null;
            csv.Configuration.MissingFieldFound = null;
            var bars = csv.GetRecords<RangeBarModel>().ToArray();
            FixTimestamp(config, bars);
            var ordered = bars.OrderBy(x => x.TimestampDate).ToArray();
            return ordered;
        }

        private static void FixTimestamp(BacktestConfig config, RangeBarModel[] bars)
        {
            if (string.IsNullOrWhiteSpace(config.TimestampType) || config.TimestampType == "unix-sec")
            {
                // default valid timestamp format, do nothing
                return;
            }

            foreach (var bar in bars)
            {
                var t = bar.Timestamp;
                var d = config.TimestampDecimals ?? 0;
                var converted = t;

                switch (config.TimestampType)
                {
                    case "unix-ms":
                    case "ms":
                        converted = t / (Math.Pow(10, d));
                        break;
                }

                bar.Timestamp = converted;
            }
        }

        private static string[] LoadAllFiles(string dirPath, string filePattern)
        {
            if(!Directory.Exists(dirPath))
                return new string[0];

            var files = Directory.EnumerateFiles(dirPath, filePattern);
            return files.OrderBy(x => x).ToArray();
        }

        private static void SaveTextReport(string report, BacktestConfig backtest, IStrategy strategy)
        {
            var filename = Path.GetFileName(backtest.DirectoryPath);
            var strategyName = strategy.GetType().Name;
            var pattern = ExtractFromPattern(backtest);
            pattern = string.IsNullOrWhiteSpace(pattern) ? filename : pattern;
            var targetFile = Path.Combine(GetPathToReportDir(backtest), $"{pattern}__{strategyName}.txt");
            File.WriteAllText(targetFile, report);
        }

        private static void Visualize(BacktestConfig backtest, ProfitComputer computer, IStrategy strategy, 
            int maxInv, ProfitInfo report, ProfitInfo[] days)
        {
            if (computer == null || backtest.Visualize == null || !backtest.Visualize.Value)
                return;

            var chart = new ChartVisualizer();
            var filename = Path.GetFileName(backtest.DirectoryPath);
            var strategyName = strategy.GetType().Name.ToLower();
            var pnl = report.Pnl;
            var name = $"pnl: {pnl:#.00} {backtest.QuoteSymbol} (max inv: {maxInv}) ";
            var dir = GetPathToReportDir(backtest);
            var pattern = ExtractFromPattern(backtest);
            var targetFile = Path.Combine(dir, $"{pattern}__{strategyName}__{maxInv}__{pnl:0}");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var bars = computer.Bars;
            var totalBars = bars.Length;
            if (backtest.VisualizeSkipBars.HasValue)
                bars = bars.Skip(backtest.VisualizeSkipBars.Value).ToArray();
            if (backtest.VisualizeLimitBars.HasValue)
                bars = bars.Take(backtest.VisualizeLimitBars.Value).ToArray();

            var minIndex = bars.Min(x => x.Index);
            var maxIndex = bars.Max(x => x.Index);

            var trades = computer.Trades
                .Where(x => x.BarIndex >= minIndex && x.BarIndex <= maxIndex)
                .ToArray();

            chart.Plot(name, targetFile, totalBars, bars, trades, days);
        }

        private static string GetPathToReportDir(BacktestConfig backtest)
        {
            return Path.Combine(Path.GetDirectoryName(backtest.DirectoryPath), "reports");
        }

        private static string ExtractFromPattern(BacktestConfig backtest)
        {
            var pattern = backtest.FilePattern ?? string.Empty;
            var split = pattern.Split("*");
            if (split.Length > 1)
                return split[0]
                    .Trim('_')
                    .Trim('_')
                    .Trim('/')
                    .Trim('\\')
                    .Trim();
            return pattern;
        }
    }
}
