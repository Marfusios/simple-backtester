using System;
using System.Collections.Generic;
using System.Linq;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class WindowStrategy : ITakerStrategy
    {
        private readonly int _window;
        private readonly int _shortWindow;
        private readonly double _fractalLimit; // = 0.90;

        private readonly Queue<RangeBarModel> _bars = new Queue<RangeBarModel>();

        private double _currentPositionEntryTimeSpan;
        private double _currentPositionExitTimeSpan;

        private double _valueMaxBuy;
        private double _valueMinBuy = double.MaxValue;

        private double _valueMaxSell;
        private double _valueMinSell = double.MaxValue;

        public WindowStrategy(int window, int shortWindow, double fractalLimit)
        {
            _window = window;
            _shortWindow = shortWindow;
            _fractalLimit = fractalLimit;
        }

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            var sellIsMin = true;

            //var valueFuncBuy = new Func<RangeBarModel, double>(x => x.ObUpdatedCountBid);
            //var valueFuncSell = new Func<RangeBarModel, double>(x => x.ObUpdatedCountAsk);
            //var valueFuncBuy = new Func<RangeBarModel, double>(x => x.SellCount);
            //var valueFuncSell = new Func<RangeBarModel, double>(x => x.BuyCount);
            var valueFuncBuy = new Func<RangeBarModel, double>(x => x.CurrentPrice);
            var valueFuncSell = new Func<RangeBarModel, double>(x => x.CurrentPrice);

            //var valueFuncBuyNorm = new Func<RangeBarModel, double>(x => Normalize(valueFuncBuy(x), _valueMaxBuy, _valueMinBuy));
            //var valueFuncSellNorm = new Func<RangeBarModel, double>(x => Normalize(valueFuncSell(x), _valueMaxSell, _valueMinSell));
            var valueFuncBuyNorm = new Func<RangeBarModel, double>(x => valueFuncBuy(x));
            var valueFuncSellNorm = new Func<RangeBarModel, double>(x => valueFuncSell(x));
            
            var currentBar = bar;
            var currentBuyValue = valueFuncBuy(currentBar);
            var currentSellValue = valueFuncSell(currentBar);

            _valueMaxBuy = Math.Max(_valueMaxBuy, currentBuyValue);
            _valueMinBuy = Math.Min(_valueMinBuy, currentBuyValue);

            _valueMaxSell = Math.Max(_valueMaxSell, currentSellValue);
            _valueMinSell = Math.Min(_valueMinSell, currentSellValue);

            var currentBuyValueNorm = valueFuncBuyNorm(currentBar);
            var currentSellValueNorm = valueFuncSellNorm(currentBar);

            var hasPosition = Math.Abs(inventoryAbsolute) > 0;

            _bars.Enqueue(bar);
            if (_bars.Count <= _window)
            {
                // waiting for initial bars
                return Action.Nothing;
            }

            _bars.Dequeue();

            var shortBars = _bars
                .Reverse()
                .Take(_shortWindow)
                .ToArray();

            //var valueFuncBuy = new Func<RangeBarModel, double>(x => x.CurrentPrice);
            //var valueFuncSell = new Func<RangeBarModel, double>(x => x.CurrentPrice);

            

            var fractalBuy = FractalEfficiency(shortBars, valueFuncBuyNorm);
            var fractalSell = FractalEfficiency(shortBars, valueFuncSellNorm);

            var maxMid = _bars.Max(x => valueFuncBuyNorm(x));
            var minMid = sellIsMin ?  
                _bars.Min(x => valueFuncSellNorm(x)) :
                _bars.Max(x => valueFuncSellNorm(x));



            //if (hasPosition)
            //{
            //    // exit
            //    _currentPositionExitTimeSpan += currentBar.TimestampDiffMs;

            //    //if (_currentPositionExitTimeSpan >= _currentPositionEntryTimeSpan)
            //    //{
            //    //    if (inventoryAbsolute > 0)
            //    //        return Action.Sell;
            //    //    if (inventoryAbsolute < 0)
            //    //        return Action.Buy;
            //    //}

            //    var exitFractalLimit = 0.4;

            //    if (_currentPositionExitTimeSpan >= _currentPositionEntryTimeSpan && fractal > exitFractalLimit)
            //    {
            //        if (inventoryAbsolute > 0)
            //            return Action.Sell;
            //        if (inventoryAbsolute < 0)
            //            return Action.Buy;
            //    }

            //    //return Action.Nothing;
            //}

            //if (currentBuyValueNorm >= maxMid && fractalBuy > _fractalLimit)
            //{
            //    // entry
            //    _currentPositionEntryTimeSpan = shortBars.Sum(x => x.TimestampDiffMs);
            //    _currentPositionExitTimeSpan = 0;
            //    return Action.Buy;
            //}

            //var sellCross = sellIsMin ? currentSellValueNorm <= minMid : currentSellValueNorm >= minMid;
            //if (sellCross && fractalSell > _fractalLimit)
            //{
            //    // entry
            //    _currentPositionEntryTimeSpan = shortBars.Sum(x => x.TimestampDiffMs);
            //    _currentPositionExitTimeSpan = 0;
            //    return Action.Sell;
            //}

            if (currentBuyValue >= maxMid)
            {
                _bars.Clear();
                return Action.Sell;
            }

            if (currentSellValue <= minMid)
            {
                _bars.Clear();
                return Action.Buy;
            }

            return Action.Nothing;
        }

        private double FractalEfficiency(RangeBarModel[] bars, Func<RangeBarModel, double> valueFunc)
        {
            var count = bars.Length;
            var absPath = Math.Abs(valueFunc(bars[count - 1]) - valueFunc(bars[0]));
            var diff = ComputeDiff(bars, valueFunc);
            var sumPath = diff.Select(Math.Abs).Sum();
            //return Math.Round(absPath / sumPath, 4);
            return absPath / sumPath;
        }

        private IEnumerable<double> ComputeDiff(RangeBarModel[] bars, Func<RangeBarModel, double> valueFunc)
        {
            for (int i = bars.Length-1; i > 0; i--)
            {
                var current = bars[i];
                var before = bars[i - 1];
                yield return valueFunc(current) - valueFunc(before);
            }
        }

        private double Normalize(double value, double max, double min)
        {
            var den = max - min;
            var normalized = den >  0 || den < 0 ? (value - min) / (den) : 0;

            if (normalized < 0 || normalized > 1)
            {
                // remove extremes
                normalized = Math.Max(0, normalized);
                normalized = Math.Min(1, normalized);
            }

            return normalized;
        }
    }
}
