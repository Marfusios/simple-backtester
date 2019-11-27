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

        private readonly string _quoteSymbol;
        private readonly string _baseSymbol;
        private readonly double _orderSize;
        private readonly double _feePercentage;
        private readonly int? _maxLimitInventory;

        private readonly List<TradeModel> _trades = new List<TradeModel>();
        private readonly List<RangeBarModel> _bars = new List<RangeBarModel>();

        private int _currentInventory;
        private int _maxInventory;

        public ProfitComputer(string baseSymbol, string quoteSymbol, double orderSize, 
            IStrategy strategy, double feePercentage, int? maxLimitInventory)
        {
            _orderSize = orderSize;
            _strategy = strategy;
            _feePercentage = feePercentage;
            _maxLimitInventory = maxLimitInventory;
            _quoteSymbol = quoteSymbol;
            _baseSymbol = baseSymbol;
        }

        public TradeModel[] Trades => _trades.ToArray();
        public RangeBarModel[] Bars => _bars.ToArray();

        public void ProcessBars(RangeBarModel[] bars)
        {
            foreach (var bar in bars)
            {
                _bars.Add(bar);

                var decision = _strategy.Decide(bar);
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
                        Amount = orderSize
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
                        Amount = orderSize * (-1)
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
                    Amount = Math.Abs(_currentInventory * _orderSize) * (-1)
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
                    Amount = Math.Abs(_currentInventory * _orderSize)
                };
                _trades.Add(trade);
            }
        }

        public string GetReport()
        {
            var buys = _trades.Where(x => x.Amount > 0).ToArray();
            var sells = _trades.Where(x => x.Amount < 0).ToArray();

            var bought = buys.Sum(x => x.Price * Math.Abs(x.Amount));
            var sold = sells.Sum(x => x.Price * Math.Abs(x.Amount));

            var boughtAmount = buys.Sum(x => Math.Abs(x.Amount));
            var soldAmount = sells.Sum(x => Math.Abs(x.Amount));

            var avgBuy = bought / Math.Max(boughtAmount, 1);
            var avgSell = sold / Math.Max(soldAmount, 1);

            var pnl = sold - bought;

            var fee = bought * _feePercentage + sold * _feePercentage;
            var pnlWithFee = pnl - fee;

            return $"Trades {_trades.Count} " +
                   $"(b: {buys.Length}/{avgBuy:#.00} {_quoteSymbol}, s: {sells.Length}/{avgSell:#.00} {_quoteSymbol}), " +
                   $"Inv: {_currentInventory*_orderSize} {_baseSymbol} (max: {_maxInventory*_orderSize} {_baseSymbol}), " +
                   $"Pnl: {pnl:#.00} {_quoteSymbol} (with fee: {pnlWithFee:#.00} {_quoteSymbol})";
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

        private void LogTrade(TradeModel trade)
        {
            var side = trade.Amount < 0 ? "SELL" : "BUY";

            //Console.WriteLine();
        }
    }
}
