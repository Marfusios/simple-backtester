using System;
using System.Collections.Generic;
using System.Linq;
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

        private int _currentInventory;
        private int _maxInventory;

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

        public void ProcessBars(RangeBarModel[] bars)
        {
            foreach (var bar in bars)
            {
                bar.Index = _bars.Count;
                _bars.Add(bar);

                var invAbs = _currentInventory * _orderSize;
                var decision = _strategy.Decide(bar, invAbs);
                if(decision == Action.Nothing)
                    continue;

                var orderSize = _orderSize;
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
                            continue;
                        }
                        if (_currentInventory > 0)
                            positionState = PositionState.Increase;
                        _currentInventory++;
                    }

                    var trade = new TradeModel()
                    {
                        Timestamp = bar.Timestamp,
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
                            continue;
                        }
                        if (_currentInventory < 0)
                            positionState = PositionState.Increase;
                        _currentInventory--;
                    }

                    
                    var trade = new TradeModel()
                    {
                        Timestamp = bar.Timestamp,
                        Price = bar.Bid ?? bar.CurrentPrice,
                        Amount = orderSize * (-1),
                        BarIndex = _bars.Count,
                        CurrentInventory = _currentInventory,
                        PositionState = positionState
                    };
                    _trades.Add(trade);
                    LogTrade(trade);
                }

                _maxInventory = Math.Max(_maxInventory, Math.Abs(_currentInventory));
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
                    Timestamp = bar.Timestamp,
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
                    Timestamp = bar.Timestamp,
                    Price = bar.Ask ?? bar.CurrentPrice,
                    Amount = Math.Abs(_currentInventory * _orderSize),
                    BarIndex = _bars.Count,
                    CurrentInventory = 0,
                    PositionState = PositionState.Close
                };
                _trades.Add(trade);
            }
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

        public ProfitInfo[] GetReportPerDays(int year, int month)
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

                var formatted = $"day:   {group.Key:00}, {dayReport}";
                dayReport.Report = formatted;
                dayReport.Day = group.Key;
                dayReport.Year = year;
                dayReport.Month = month;
                reports.Add(dayReport);

                if (total == null)
                {
                    total = dayReport.Clone();
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

        public ProfitInfo GetTotalReport(ProfitInfo[] profits)
        {
            var total = new ProfitInfo();

            foreach (var profit in profits)
            {
                total.TradesCount += profit.TradesCount;
                total.BuysCount += profit.BuysCount;
                total.SellsCount += profit.SellsCount;
                total.TotalBought += profit.TotalBought;
                total.TotalSold += profit.TotalSold;
                total.TotalBoughtQuote += profit.TotalBoughtQuote;
                total.TotalSoldQuote += profit.TotalSoldQuote;
                total.Pnl += profit.Pnl;
                total.PnlNoExcess += profit.PnlNoExcess;
                total.PnlWithFee += profit.PnlWithFee;
                total.DisplayWithFee = profit.DisplayWithFee;
                total.QuoteSymbol = profit.QuoteSymbol;
                total.BaseSymbol = profit.BaseSymbol;
                total.MaxInventory = profit.MaxInventory;
                total.MaxInventoryLimit = profit.MaxInventoryLimit;
                total.OrderSize = profit.OrderSize;
            }

            total.AverageBuyPrice = total.TotalBoughtQuote / total.TotalBought;
            total.AverageSellPrice = total.TotalSoldQuote / total.TotalSold;

            var formattedTotal = $"{total} with excess";
            total.Report = formattedTotal;
            total.Day = null;
            total.Month = null;
            total.Year = null;
            return total;
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
            var side = trade.Amount < 0 ? "SELL" : "BUY";

            //Console.WriteLine();
        }
    }
}
