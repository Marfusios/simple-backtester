using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimpleBacktester.Data;
using SimpleBacktester.Strategies;
using Action = SimpleBacktester.Strategies.Action;

namespace SimpleBacktester
{
    public class ProfitComputer
    {
        private readonly IStrategy _strategy;

        private readonly BacktestConfig _config;
        private readonly string _quoteSymbol;
        private readonly string _baseSymbol;
        private readonly double _orderSize;
        private readonly double _feePercentage;
        private readonly int? _maxLimitInventory;

        private readonly List<TradeModel> _trades = new List<TradeModel>();
        private readonly List<RangeBarModel> _bars = new List<RangeBarModel>();

        private double _currentInventoryAbs;
        private int _currentInventory;
        private int _maxInventory;

        private List<PlacedOrder> _placedOrders = new List<PlacedOrder>();

        public ProfitComputer(IStrategy strategy, BacktestConfig config, int? maxLimitInventory)
        {
            _orderSize = config.Amount ?? 1;
            _strategy = strategy;
            _feePercentage = config.FeePercentage ?? 0;
            _quoteSymbol = config.QuoteSymbol;
            _baseSymbol = config.BaseSymbol;
            _config = config;
            _maxLimitInventory = maxLimitInventory;
        }

        public TradeModel[] Trades => _trades.ToArray();
        public RangeBarModel[] Bars => _bars.ToArray();

        public double InitialPrice => _bars.FirstOrDefault()?.CurrentPrice ?? 0;

        public void ProcessBars(RangeBarModel[] bars)
        {
            foreach (var bar in bars)
            {
                bar.Index = _bars.Count;
                _bars.Add(bar);

                ExecuteTakerStrategy(_strategy as ITakerStrategy, bar, _orderSize);
                ExecuteMakerStrategy(_strategy as IMakerStrategy, bar);

                _maxInventory = Math.Max(_maxInventory, Math.Abs(_currentInventory));
            }
        }

        private bool ExecuteTakerStrategy(ITakerStrategy strategy, RangeBarModel bar, double orderSize)
        {
            if (strategy == null)
                return false;

            var decision = strategy.Decide(bar, _currentInventoryAbs);
            if (decision == Action.Nothing)
                return true;

            var positionState = PositionState.Open;

            if (decision == Action.Buy)
            {
                if (_currentInventory < 0)
                {
                    orderSize = Math.Abs(_currentInventory) * orderSize;
                    _currentInventory = 0;
                    positionState = PositionState.Close;
                }
                else
                {
                    if (_maxLimitInventory.HasValue && Math.Abs(_currentInventory) >= _maxLimitInventory.Value)
                    {
                        // inventory reached, do nothing
                        return true;
                    }

                    if (_currentInventory > 0)
                        positionState = PositionState.Increase;
                    _currentInventory++;
                }

                _currentInventoryAbs += orderSize;
                var trade = new TradeModel()
                {
                    Timestamp = bar.TimestampUnix,
                    Price = bar.Ask ?? bar.CurrentPrice,
                    Amount = orderSize,
                    BarIndex = _bars.Count,
                    CurrentInventory = _currentInventory,
                    PositionState = positionState
                };
                _trades.Add(trade);
                LogTrade(trade);
            }

            if (decision == Action.Sell)
            {
                if (_currentInventory > 0)
                {
                    orderSize = _currentInventory * orderSize;
                    _currentInventory = 0;
                    positionState = PositionState.Close;
                }
                else
                {
                    if (_maxLimitInventory.HasValue && Math.Abs(_currentInventory) >= _maxLimitInventory.Value)
                    {
                        // inventory reached, do nothing
                        return true;
                    }

                    if (_currentInventory < 0)
                        positionState = PositionState.Increase;
                    _currentInventory--;
                }

                _currentInventoryAbs -= orderSize;
                var trade = new TradeModel()
                {
                    Timestamp = bar.TimestampUnix,
                    Price = bar.Bid ?? bar.CurrentPrice,
                    Amount = orderSize * (-1),
                    BarIndex = _bars.Count,
                    CurrentInventory = _currentInventory,
                    PositionState = positionState
                };
                _trades.Add(trade);
                LogTrade(trade);
            }

            return false;
        }

        private bool ExecuteMakerStrategy(IMakerStrategy strategy, RangeBarModel bar)
        {
            if (strategy == null)
                return false;

            EvaluatePreviousOrders(bar);

            var newPlacedOrders = strategy.Decide(bar, _currentInventoryAbs, _placedOrders.ToArray());
            
            _placedOrders.Clear();
            _placedOrders.AddRange(newPlacedOrders);

            return false;
        }

        private void EvaluatePreviousOrders(RangeBarModel bar)
        {
            if (_placedOrders.Count <= 0)
                return;

            var highPrice = bar.HighBuy ?? bar.High;
            var lowPrice = bar.LowSell ?? bar.Low;

            foreach (var placedOrder in _placedOrders.ToArray())
            {
                var orderPrice = placedOrder.Price;
                var orderAmount = placedOrder.Amount;
                var positionState = PositionState.Open;

                if (placedOrder.Side == OrderSide.Bid)
                {
                    if (_currentInventoryAbs < -1e-6)
                    {
                        positionState = PositionState.Close;
                    }
                    else
                    {
                        if (_maxLimitInventory.HasValue && Math.Abs(_currentInventory) >= _maxLimitInventory.Value)
                        {
                            // inventory reached, do nothing
                            _placedOrders.Remove(placedOrder);
                            continue;
                        }

                        if (_currentInventoryAbs > 1e-6)
                            positionState = PositionState.Increase;
                    }

                    if (lowPrice == null || lowPrice > orderPrice)
                    {
                        // no BUY trade, price was too high
                        continue;
                    }

                    _currentInventory++;
                    _currentInventoryAbs += orderAmount;
                    var trade = new TradeModel
                    {
                        Timestamp = bar.TimestampUnix,
                        Price = orderPrice,
                        Amount = orderAmount,
                        BarIndex = _bars.Count,
                        CurrentInventory = _currentInventory,
                        PositionState = positionState
                    };
                    _trades.Add(trade);
                    LogTrade(trade);
                    _placedOrders.Remove(placedOrder);
                }
                else
                {
                    if (_currentInventoryAbs > 1e-6)
                    {
                        positionState = PositionState.Close;
                    }
                    else
                    {
                        if (_maxLimitInventory.HasValue && Math.Abs(_currentInventory) >= _maxLimitInventory.Value)
                        {
                            // inventory reached, do nothing
                            _placedOrders.Remove(placedOrder);
                            continue;
                        }

                        if (_currentInventoryAbs < -1e-6)
                            positionState = PositionState.Increase;
                        
                    }

                    if (highPrice == null || highPrice < orderPrice)
                    {
                        // no SELL trade, price was too low
                        continue;
                    }

                    _currentInventory--;
                    _currentInventoryAbs -= orderAmount;
                    var trade = new TradeModel()
                    {
                        Timestamp = bar.TimestampUnix,
                        Price = orderPrice,
                        Amount = orderAmount * (-1),
                        BarIndex = _bars.Count,
                        CurrentInventory = _currentInventory,
                        PositionState = positionState
                    };
                    _trades.Add(trade);
                    LogTrade(trade);
                    _placedOrders.Remove(placedOrder);
                }
            }
        }

        public void ProcessLastBar(RangeBarModel bar)
        {
            // reduce inventory
            if (_currentInventory > 0)
            {
                // sell trade
                var trade = new TradeModel()
                {
                    Timestamp = bar.TimestampUnix,
                    Price = bar.Bid ?? bar.CurrentPrice,
                    Amount = Math.Abs(_currentInventory * _orderSize) * (-1),
                    BarIndex = _bars.Count,
                    CurrentInventory = 0,
                    PositionState = PositionState.Close
                };
                _trades.Add(trade);
            }
            if (_currentInventory < 0)
            {
                // buy trade
                var trade = new TradeModel()
                {
                    Timestamp = bar.TimestampUnix,
                    Price = bar.Ask ?? bar.CurrentPrice,
                    Amount = Math.Abs(_currentInventory * _orderSize),
                    BarIndex = _bars.Count,
                    CurrentInventory = 0,
                    PositionState = PositionState.Close
                };
                _trades.Add(trade);
            }

            //MakeAnalysis();
        }

        public ProfitInfo GetReport()
        {
            var rep = GetReport(_trades.ToArray(), null);
            return rep;
        }

        public ProfitInfo[] GetReportByMonth()
        {
            var grouped = _trades
                .GroupBy(x => new { x.TimestampDate.Year, x.TimestampDate.Month })
                .ToArray();
            var reports = new List<ProfitInfo>();
            var reportsDay = new List<ProfitInfo>();

            for (int i = 0; i < grouped.Length; i++)
            {
                var group = grouped[i];
                var nextTrades = grouped
                    .Skip(i+1)
                    .SelectMany(x => x)
                    .OrderBy(x => x.TimestampDate)
                    .ToArray();

                var trades = group.ToArray();
                var monthReport = GetReport(trades.ToArray(), nextTrades);
                if(monthReport == ProfitInfo.Empty)
                    continue;

                var formatted = $"month: {group.Key.Month:00}/{group.Key.Year}, {monthReport}";
                monthReport.Report = formatted;
                monthReport.Year = group.Key.Year;
                monthReport.Month = group.Key.Month;
                reports.Add(monthReport);
            }

            return reports.ToArray();
        }

        public ProfitInfo[] GetReportPerDays(int year, int month, ref double maxTotalPnl, ref double minTotalPnl, ref double totalPnl)
        {
            var grouped = _trades
                .Where(x => x.TimestampDate.Year == year && x.TimestampDate.Month == month)
                .GroupBy(x => x.TimestampDate.Day)
                .ToArray();
            var reports = new List<ProfitInfo>();

            ProfitInfo total = null;

            for (int i = 0; i < grouped.Length; i++)
            {
                var group = grouped[i];
                var last = group.Last();
                var nextTrades = _trades
                    .Where(x => x.TimestampDate > last.TimestampDate)
                    .OrderBy(x => x.TimestampDate)
                    .ToArray();

                var trades = group.ToArray();
                var dayReport = GetReport(trades.ToArray(), nextTrades);
                if (dayReport == ProfitInfo.Empty)
                    continue;

                if (total == null)
                {
                    total = dayReport.Clone();
                    total.Pnl += totalPnl;
                    total.AverageBuyPrice = 0;
                    total.AverageSellPrice = 0;
                }
                else
                {
                    total.TradesCount += dayReport.TradesCount;
                    total.BuysCount += dayReport.BuysCount;
                    total.SellsCount += dayReport.SellsCount;
                    total.TotalBought += dayReport.TotalBought;
                    total.TotalSold += dayReport.TotalSold;
                    total.TotalBoughtQuote += dayReport.TotalBoughtQuote;
                    total.TotalSoldQuote += dayReport.TotalSoldQuote;
                    total.Pnl += dayReport.Pnl;
                    total.PnlNoExcess += dayReport.PnlNoExcess;
                    total.PnlWithFee += dayReport.PnlWithFee;
                }

                var previousMaxPnl = maxTotalPnl;
                maxTotalPnl = Math.Max(maxTotalPnl, total.Pnl);

                if (maxTotalPnl > previousMaxPnl)
                {
                    minTotalPnl = maxTotalPnl;
                }

                minTotalPnl = Math.Min(minTotalPnl, total.Pnl);

                var drawdown = (minTotalPnl - maxTotalPnl) / maxTotalPnl;
                if (drawdown < 0)
                    dayReport.MaxDrawdownPercentage = drawdown;

                if (totalPnl <= 0)
                {
                    totalPnl = total.Pnl;
                }
                else
                {
                    dayReport.ProfitPercentage = (total.Pnl - totalPnl) / totalPnl;
                    totalPnl = total.Pnl;
                }

                var formatted = $"day:   {group.Key:00}, {dayReport}";
                dayReport.Report = formatted;
                dayReport.Day = group.Key;
                dayReport.Year = year;
                dayReport.Month = month;
                reports.Add(dayReport);
            }

            //if (total != null)
            //{
            //    var formattedTotal = $"total: __, {total}";
            //    total.Report = formattedTotal;
            //    total.Day = null;
            //    reports.Add(total);
            //}

            return reports.ToArray();
        }

        private ProfitInfo GetReport(TradeModel[] trades, TradeModel[] nextTrades)
        {
            nextTrades ??= Array.Empty<TradeModel>();

            var cleanTrades = RemoveOpenedTrades(trades);
            if (!cleanTrades.Any())
                return ProfitInfo.Empty;

            var closing = GetClosingTrades(nextTrades);
            var withCloses = cleanTrades.Concat(closing).ToArray();

            var info = StatsComputer.ComputeProfitComplex(withCloses, 0);
            var infoWithFee = StatsComputer.ComputeProfitComplex(withCloses, _feePercentage);

            info.BaseSymbol = _baseSymbol;
            info.QuoteSymbol = _quoteSymbol;
            info.DisplayWithFee = _config.DisplayFee ?? false;
            info.OrderSize = _orderSize;
            info.CurrentInventory = _currentInventory;
            info.MaxInventory = _maxInventory;
            info.MaxInventoryLimit = _maxLimitInventory;

            info.PnlWithFee = infoWithFee.Pnl;

            return info;
        }

        private TradeModel[] RemoveOpenedTrades(TradeModel[] trades)
        {
            var firstOpenIndex = trades.FirstOrDefault(x => x.PositionState == PositionState.Open);
            if(firstOpenIndex == null)
                return new TradeModel[0];

            return trades
                .SkipWhile(x => x != firstOpenIndex)
                .ToArray();
        }

        private TradeModel[] GetClosingTrades(TradeModel[] trades)
        {
            var firstOpenIndex = trades.FirstOrDefault(x => x.PositionState == PositionState.Open);
            if (firstOpenIndex == null)
            {
                // all closing
                return trades;
            }

            return trades
                .TakeWhile(x => x != firstOpenIndex)
                .ToArray();
        }


        private void LogTrade(TradeModel trade)
        {
            //var side = trade.Amount < 0 ? "SELL" : "BUY";
            //Console.WriteLine();
        }

        private void MakeAnalysis()
        {
            var totalBars = _bars.ToArray();

            var groupedPerDay = totalBars
                .GroupBy(x => x.TimestampDate.Date)
                .ToArray();
            var groupedPerMonth = totalBars
                .GroupBy(x => new DateTime(x.TimestampDate.Year, x.TimestampDate.Month, 1))
                .ToArray();
            var groupedPerYear = totalBars
                .GroupBy(x => new DateTime(x.TimestampDate.Year, 12, 1))
                .ToArray();

            var fileName = "C:\\dev\\data\\analysis\\range_bars.txt";
            var builder = new StringBuilder();

            builder.AppendLine("Range Bars ANALYSIS");
            builder.AppendLine();
            builder.AppendLine(
                $"Bars: {totalBars.Length}, start: {totalBars.First().TimestampDate:D}, end: {totalBars.Last().TimestampDate:D}");
            builder.AppendLine();

            builder.AppendLine("YEARS");
            AnalyzeGroup(groupedPerYear, builder);

            builder.AppendLine();
            builder.AppendLine("MONTHS");
            AnalyzeGroup(groupedPerMonth, builder);

            builder.AppendLine();
            builder.AppendLine("DAYS");
            AnalyzeGroup(groupedPerDay, builder);

            File.WriteAllText(fileName, builder.ToString());
        }

        private void AnalyzeGroup(IGrouping<DateTime, RangeBarModel>[] grouped, StringBuilder builder)
        {
            foreach (var group in grouped)
            {
                var barsPerDay = @group
                    .OrderBy(x => x.TimestampDate)
                    .ToArray();

                builder.Append($"{@group.Key:MM/dd/yyyy}  ");

                var timeDiffMean = barsPerDay.Average(x => x.TimestampDiffMs) / 1000 / 60;
                var timeDiffMedian = barsPerDay.Median(x => x.TimestampDiffMs) / 1000 / 60;

                var uptickUptick = 0;
                var uptickDowntick = 0;
                var uptickTotal = 0;

                var downtickUptick = 0;
                var downtickDowntick = 0;
                var downtickTotal = 0;

                var uptickUptickUptick = 0;
                var uptickUptickDowntick = 0;
                var uptickDowntickUptick = 0;
                var uptickDowntickDowntick = 0;

                var downtickUptickUptick = 0;
                var downtickUptickDowntick = 0;
                var downtickDowntickUptick = 0;
                var downtickDowntickDowntick = 0;
                
                var uuTotal = 0;
                var udTotal = 0;
                var duTotal = 0;
                var ddTotal = 0;

                for (int i = 0; i < barsPerDay.Length; i++)
                {
                    var current = barsPerDay[i];
                    var previous = i > 0 ? barsPerDay[i - 1] : current;
                    var prePrevious = i > 1 ? barsPerDay[i - 2] : previous;

                    var currentChange = Math.Sign(current.MidChange ?? 0);
                    var previousChange = Math.Sign(previous.MidChange ?? 0);
                    var prePreviousChange = Math.Sign(prePrevious.MidChange ?? 0);

                    if (previousChange > 0)
                    {
                        uptickTotal++;
                        if (currentChange > 0) uptickUptick++;
                        if (currentChange < 0) uptickDowntick++;

                        if (prePreviousChange > 0)
                        {
                            uuTotal++;
                            if (currentChange > 0) uptickUptickUptick++;
                            if (currentChange < 0) uptickUptickDowntick++;
                        }

                        if (prePreviousChange < 0)
                        {
                            duTotal++;
                            if (currentChange > 0) downtickUptickUptick++;
                            if (currentChange < 0) downtickUptickDowntick++;
                        }
                    }

                    if (previousChange < 0)
                    {
                        downtickTotal++;
                        if (currentChange > 0) downtickUptick++;
                        if (currentChange < 0) downtickDowntick++;

                        if (prePreviousChange > 0)
                        {
                            udTotal++;
                            if (currentChange > 0) uptickDowntickUptick++;
                            if (currentChange < 0) uptickDowntickDowntick++;
                        }

                        if (prePreviousChange < 0)
                        {
                            ddTotal++;
                            if (currentChange > 0) downtickDowntickUptick++;
                            if (currentChange < 0) downtickDowntickDowntick++;
                        }
                    }
                }

                var totalChanged = uptickTotal + downtickTotal;
                var total = barsPerDay.Length;

                builder.Append($"bars: {total:0000} (price unchanged: {total - totalChanged}),  ");

                builder.Append($"UU: {ComputePercentage(uptickTotal, uptickUptick):F}%, ");
                builder.Append($"UD: {ComputePercentage(uptickTotal, uptickDowntick):F}%, ");
                builder.Append($"DU: {ComputePercentage(downtickTotal, downtickUptick):F}%, ");
                builder.Append($"DD: {ComputePercentage(downtickTotal, downtickDowntick):F}%   | ");

                builder.Append($"UUU: {ComputePercentage(uuTotal, uptickUptickUptick):F}%, ");
                builder.Append($"UUD: {ComputePercentage(uuTotal, uptickUptickDowntick):F}%, ");
                builder.Append($"UDU: {ComputePercentage(udTotal, uptickDowntickUptick):F}%, ");
                builder.Append($"UDD: {ComputePercentage(udTotal, uptickDowntickDowntick):F}%  ---  ");

                builder.Append($"DUU: {ComputePercentage(duTotal, downtickUptickUptick):F}%, ");
                builder.Append($"DUD: {ComputePercentage(duTotal, downtickUptickDowntick):F}%, ");
                builder.Append($"DDU: {ComputePercentage(ddTotal, downtickDowntickUptick):F}%, ");
                builder.Append($"DDD: {ComputePercentage(ddTotal, downtickDowntickDowntick):F}%   | ");

                builder.Append($"time diff: {timeDiffMean:F} min (median: {timeDiffMedian:F} min)");
                builder.AppendLine();
            }
        }

        private double ComputePercentage(int total, int value)
        {
            return (value / (double)total) * 100;
        }
    }
}
