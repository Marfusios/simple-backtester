using System;

namespace RangeBarProfit.Strategies
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

        public TrendStrategy(bool aggressive)
        {
            _aggressive = aggressive;
        }

        public Action Decide(RangeBarModel bar, double inventory)
        {
            if (_lastBar == null)
            {
                _lastBar = bar;
                return Action.Nothing;
            }

            _lastMidChange = bar.Mid - _lastBar.Mid;
            _lastBar = bar;

            if (Math.Abs(_lastMidChange) < 0.00001)
                return Action.Nothing;

            if (_aggressive)
                return DecideAggressive(bar, _lastMidChange, inventory);
            return DecideConservative(bar, _lastMidChange);
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

        private Action DecideConservative(RangeBarModel bar, double midChange)
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
