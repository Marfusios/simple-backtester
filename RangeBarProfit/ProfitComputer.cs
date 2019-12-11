using System;
using System.Collections.Generic;
using System.Linq;
using RangeBarProfit.Strategies;
using Action = RangeBarProfit.Strategies.Action;

namespace RangeBarProfit
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
            _orderSize = config.Amount;
            _strategy = strategy;
            _feePercentage = config.FeePercentage;
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

                if (decision == Action.Buy)
                {
                    if (_currentInventory < 0)
                    {
                        orderSize = Math.Abs(_currentInventory) * orderSize;
                        _currentInventory = 0;
                    }
                    else
                    {
                        if (_maxLimitInventory.HasValue && Math.Abs(_currentInventory) >= _maxLimitInventory.Value)
                        {
                            // inventory reached, do nothing
                            continue;
                        }
                        _currentInventory++;
                    }

                    var trade = new TradeModel()
                    {
                        Timestamp = bar.Timestamp,
                        Price = bar.Ask,
                        Amount = orderSize,
                        BarIndex = _bars.Count
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
                    }
                    else
                    {
                        if (_maxLimitInventory.HasValue && Math.Abs(_currentInventory) >= _maxLimitInventory.Value)
                        {
                            // inventory reached, do nothing
                            continue;
                        }
                        _currentInventory--;
                    }

                    
                    var trade = new TradeModel()
                    {
                        Timestamp = bar.Timestamp,
                        Price = bar.Bid,
                        Amount = orderSize * (-1),
                        BarIndex = _bars.Count
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
                    Price = bar.Bid,
                    Amount = Math.Abs(_currentInventory * _orderSize) * (-1),
                    BarIndex = _bars.Count
                };
                _trades.Add(trade);
            }
            if (_currentInventory < 0)
            {
                // buy trade
                var trade = new TradeModel()
                {
                    Timestamp = bar.Timestamp,
                    Price = bar.Bid,
                    Amount = Math.Abs(_currentInventory * _orderSize),
                    BarIndex = _bars.Count
                };
                _trades.Add(trade);
            }
        }

        public string GetReport()
        {
            var rep = GetReport(_trades.ToArray(), _bars.ToArray());
            return rep.ToString();
        }

        public string[] GetReportByMonth()
        {
            var grouped = _trades.GroupBy(x => x.TimestampDate.Month);
            var reports = new List<string>();

            ProfitInfo total = null;

            foreach (var group in grouped)
            {
                var bars = _bars
                    .Where(x => x.TimestampDate.Month == group.Key+1)
                    .OrderBy(x => x.Timestamp)
                    .Take(1)
                    .ToArray();

                if(!bars.Any())
                    bars = _bars
                        .Where(x => x.TimestampDate.Month == group.Key)
                        .OrderBy(x => x.Timestamp)
                        .ToArray();

                var trades = group.ToArray();
                var monthReport = GetReport(trades.ToArray(), bars);
                var formatted = $"month: {group.Key:00}, {monthReport}";
                reports.Add(formatted);

                if (total == null)
                {
                    total = monthReport;
                    total.AverageBuy = 0;
                    total.AverageSell = 0;
                }
                else
                {
                    total.TradesCount += monthReport.TradesCount;
                    total.BuysCount += monthReport.BuysCount;
                    total.SellsCount += monthReport.SellsCount;
                    total.Pnl += monthReport.Pnl;
                    total.PnlWithFee += monthReport.PnlWithFee;
                }
            }

            var formattedTotal = $"total: __, {total}";
            reports.Add(formattedTotal);

            return reports.ToArray();
        }

        public double GetPnl()
        {
            var buys = _trades.Where(x => x.Amount > 0).ToArray();
            var sells = _trades.Where(x => x.Amount < 0).ToArray();

            var bought = buys.Sum(x => x.Price * Math.Abs(x.Amount));
            var sold = sells.Sum(x => x.Price * Math.Abs(x.Amount));

            var pnl = sold - bought;
            return pnl;
        }

        private ProfitInfo GetReport(TradeModel[] trades, RangeBarModel[] bars)
        {
            var buys = trades.Where(x => x.Amount > 0).ToList();
            var sells = trades.Where(x => x.Amount < 0).ToList();

            var boughtAmount = buys.Sum(x => Math.Abs(x.Amount));
            var soldAmount = sells.Sum(x => Math.Abs(x.Amount));
            var diff = soldAmount - boughtAmount;
            var diffAbs = Math.Abs(diff);

            if (boughtAmount > soldAmount && diffAbs > 0.001)
            {
                // more bought, need to sell at last day price
                var lastBar = bars.Last();
                var trade = new TradeModel()
                {
                    Timestamp = lastBar.Timestamp,
                    Price = lastBar.Bid,
                    Amount = diff,
                    BarIndex = lastBar.Index
                };
                sells.Add(trade);
            }
            if (boughtAmount < soldAmount && diffAbs > 0.001)
            {
                // more sold, need to buy at last day price
                var lastBar = bars.Last();
                var trade = new TradeModel()
                {
                    Timestamp = lastBar.Timestamp,
                    Price = lastBar.Ask,
                    Amount = diff,
                    BarIndex = lastBar.Index
                };
                buys.Add(trade);
            }

            boughtAmount = buys.Sum(x => Math.Abs(x.Amount));
            soldAmount = sells.Sum(x => Math.Abs(x.Amount));

            var bought = buys.Sum(x => x.Price * Math.Abs(x.Amount));
            var sold = sells.Sum(x => x.Price * Math.Abs(x.Amount));

            var avgBuy = bought / Math.Max(boughtAmount, 1);
            var avgSell = sold / Math.Max(soldAmount, 1);

            var pnl = sold - bought;

            var fee = bought * _feePercentage + sold * _feePercentage;
            var pnlWithFee = pnl - fee;

            var info = new ProfitInfo()
            {
                TradesCount = trades.Length,
                BuysCount =  buys.Count,
                SellsCount = sells.Count,

                AverageBuy = avgBuy,
                AverageSell = avgSell,

                BaseSymbol = _baseSymbol,
                QuoteSymbol = _quoteSymbol,

                Pnl = pnl,
                PnlWithFee = pnlWithFee,

                DisplayWithFee = _config.DisplayFee,

                OrderSize = _orderSize,
                CurrentInventory = _currentInventory,
                MaxInventory = _maxInventory,
                MaxInventoryLimit = _maxLimitInventory,
            };
            return info;
        }

        private void LogTrade(TradeModel trade)
        {
            var side = trade.Amount < 0 ? "SELL" : "BUY";

            //Console.WriteLine();
        }
    }
}
