using System;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class TrendStrategy : IStrategy
    {
        private readonly bool _aggressive;

        private int _trendBreak = 4; 
        private int _trendCounter;
        private int _trendUpCounter;
        private int _trendDownCounter;

        private bool _nextBuy;
        private bool _nextSell;

        private RangeBarModel _lastBar;
        private double _lastMidChange;
        private double _lastInventory;

        private bool _entryBuyPaused;
        private bool _entrySellPaused;

        public TrendStrategy(bool aggressive)
        {
            _aggressive = aggressive;
        }

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            if (_lastBar == null)
            {
                _lastBar = bar;
                return Action.Nothing;
            }

            _lastMidChange = bar.CurrentPrice - _lastBar.CurrentPrice;
            _lastBar = bar;

            if (Math.Abs(_lastMidChange) < 0.00001)
                return Action.Nothing;

            if (_aggressive)
                return DecideAggressive(bar, _lastMidChange, inventoryAbsolute);
            return DecideConservative(bar, _lastMidChange, inventoryAbsolute);
        }

        //private Action DecideAggressive(RangeBarModel bar, double midChange)
        //{
        //    var currentTrend = Math.Sign(midChange);
        //    var sameTrend = Math.Sign(_trendCounter) == currentTrend;

        //    if (Math.Abs(_trendCounter) >= _trendBreak)
        //    {
        //        // trend, check if current is opposite
        //        if (!sameTrend)
        //        {
        //            // opposite trend, close position
        //            _trendCounter = currentTrend;
        //            return currentTrend < 0 ? Action.Sell : Action.Buy;
        //        }
        //    }

        //    if (sameTrend)
        //        _trendCounter += currentTrend;
        //    else
        //        _trendCounter += 2 * currentTrend;
        //    return Action.Nothing;
        //}

        private Action DecideAggressive(RangeBarModel bar, double midChange, double inventory)
        {
            if (_nextBuy)
            {
                _nextBuy = false;
                return Action.Buy;
            }

            if (_nextSell)
            {
                _nextSell = false;
                return Action.Sell;
            }

            var currentTrend = Math.Sign(midChange);

            if (currentTrend > 0)
            {
                _trendUpCounter += 1;
            }

            if (currentTrend < 0)
            {
                _trendDownCounter += 1;
            }

            if (_trendUpCounter >= _trendBreak)
            {
                _trendUpCounter = 0;
                _trendDownCounter = 0;
                if (inventory < 0)
                {
                    // closing short position, force opposite side
                    _nextBuy = true;
                }
                return Action.Buy;
            }

            if (_trendDownCounter >= _trendBreak)
            {
                _trendUpCounter = 0;
                _trendDownCounter = 0;
                if (inventory > 0)
                {
                    // closing long position, force opposite side
                    _nextSell = true;
                }
                return Action.Sell;
            }

            return Action.Nothing;
        }

        //private Action DecideConservative(RangeBarModel bar, double midChange)
        //{
        //    var currentTrend = Math.Sign(midChange);

        //    if (Math.Abs(_trendCounter) >= _trendBreak)
        //    {
        //        // trend, check if current is opposite
        //        var sameTrend = Math.Sign(_trendCounter) == currentTrend;
        //        if (!sameTrend)
        //        {
        //            // opposite trend, close position
        //            _trendCounter = 0;
        //            return currentTrend < 0 ? Action.Sell : Action.Buy;
        //        }
        //    }

        //    _trendCounter += currentTrend;
        //    return Action.Nothing;
        //}

        //private Action DecideConservative(RangeBarModel bar, double midChange, double inventory)
        //{
        //    var lastInv = _lastInventory;
        //    var currentInv = inventory;
        //    _lastInventory = currentInv;

        //    if (Math.Abs(currentInv) > 0)
        //    {
        //        _entryBuyPaused = false;
        //        _entrySellPaused = false;
        //    }
        //    else
        //    {
        //        if (lastInv > 0)
        //            _entrySellPaused = true;
        //        else
        //            _entryBuyPaused = true;
        //    }

        //    var currentTrend = Math.Sign(midChange);

        //    if (Math.Abs(_trendCounter) >= _trendBreak)
        //    {
        //        // trend, check if current is opposite
        //        var sameTrend = Math.Sign(_trendCounter) == currentTrend;
        //        if (!sameTrend)
        //        {
        //            // opposite trend, close position
        //            _trendCounter = 0;
        //            var action = currentTrend < 0 ? Action.Sell : Action.Buy;
        //            if (action == Action.Buy && _entryBuyPaused)
        //                return Action.Nothing;
        //            if (action == Action.Sell && _entrySellPaused)
        //                return Action.Nothing;
        //            return action;
        //        }
        //    }

        //    _trendCounter += currentTrend;
        //    return Action.Nothing;
        //}

        private Action DecideConservative(RangeBarModel bar, double midChange, double inventory)
        {
            var currentTrend = Math.Sign(midChange);

            if (currentTrend > 0)
            {
                _trendUpCounter += 1;
            }

            if (currentTrend < 0)
            {
                _trendDownCounter += 1;
            }

            if (_trendUpCounter >= _trendBreak)
            {
                _trendUpCounter = 0;
                _trendDownCounter = 0;
                return Action.Buy;
            }

            if (_trendDownCounter >= _trendBreak)
            {
                _trendUpCounter = 0;
                _trendDownCounter = 0;
                return Action.Sell;
            }

            return Action.Nothing;
        }
    }
}
