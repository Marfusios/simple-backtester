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
            var rep = GetReport(_trades.ToArray());
            return rep;
        }

        public ProfitInfo[] GetReportByMonth()
        {
            var grouped = _trades.GroupBy(x => new { x.TimestampDate.Year, x.TimestampDate.Month });
            var reports = new List<ProfitInfo>();

            ProfitInfo total = null;

            foreach (var group in grouped)
            {
                var trades = group.ToArray();
                var monthReport = GetReport(trades.ToArray());
                var formatted = $"month: {group.Key.Month:00}/{group.Key.Year}, {monthReport}";
                monthReport.Report = formatted;
                monthReport.Year = group.Key.Year;
                monthReport.Month = group.Key.Month;
                reports.Add(monthReport);

                if (total == null)
                {
                    total = monthReport.Clone();
                    total.AverageBuy = 0;
                    total.AverageSell = 0;
                }
                else
                {
                    total.TradesCount += monthReport.TradesCount;
                    total.BuysCount += monthReport.BuysCount;
                    total.SellsCount += monthReport.SellsCount;
                    total.TotalBought += monthReport.TotalBought;
                    total.TotalSold += monthReport.TotalSold;
                    total.Pnl += monthReport.Pnl;
                    total.PnlNoExcess += monthReport.PnlNoExcess;
                    total.PnlWithFee += monthReport.PnlWithFee;
                }
            }

            if (total != null)
            {
                var formattedTotal = $"total: __, {total}";
                total.Report = formattedTotal;
                total.Month = null;
                reports.Add(total);
            }

            return reports.ToArray();
        }

        public ProfitInfo[] GetReportPerDays(int year, int month)
        {
            var grouped = _trades
                .Where(x => x.TimestampDate.Month == month)
                .GroupBy(x => x.TimestampDate.Day);
            var reports = new List<ProfitInfo>();

            ProfitInfo total = null;

            foreach (var group in grouped)
            {
                var trades = group.ToArray();
                var dayReport = GetReport(trades.ToArray());
                var formatted = $"day:   {group.Key:00}, {dayReport}";
                dayReport.Report = formatted;
                dayReport.Day = group.Key;
                dayReport.Year = year;
                dayReport.Month = month;
                reports.Add(dayReport);

                if (total == null)
                {
                    total = dayReport.Clone();
                    total.AverageBuy = 0;
                    total.AverageSell = 0;
                }
                else
                {
                    total.TradesCount += dayReport.TradesCount;
                    total.BuysCount += dayReport.BuysCount;
                    total.SellsCount += dayReport.SellsCount;
                    total.TotalBought += dayReport.TotalBought;
                    total.TotalSold += dayReport.TotalSold;
                    total.Pnl += dayReport.Pnl;
                    total.PnlNoExcess += dayReport.PnlNoExcess;
                    total.PnlWithFee += dayReport.PnlWithFee;
                }
            }

            if (total != null)
            {
                var formattedTotal = $"total: __, {total}";
                total.Report = formattedTotal;
                total.Day = null;
                reports.Add(total);
            }

            return reports.ToArray();
        }

        private ProfitInfo GetReport(TradeModel[] trades)
        {
            var cleanTrades = RemoveOpenedTrades(trades);
            var info = StatsComputer.ComputeProfitComplex(cleanTrades, 0);
            var infoWithFee = StatsComputer.ComputeProfitComplex(cleanTrades, _feePercentage);

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


        private void LogTrade(TradeModel trade)
        {
            var side = trade.Amount < 0 ? "SELL" : "BUY";

            //Console.WriteLine();
        }
    }
}
