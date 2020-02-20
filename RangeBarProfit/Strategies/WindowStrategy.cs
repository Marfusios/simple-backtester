using System;
using System.Collections.Generic;
using System.Linq;

namespace RangeBarProfit.Strategies
{
    public class WindowStrategy : IStrategy
    {
        private readonly int _window;
        private readonly int _shortWindow;

        private readonly Queue<RangeBarModel> _bars = new Queue<RangeBarModel>();

        private double _currentPositionEntryTimeSpan;
        private double _currentPositionExitTimeSpan;

        public WindowStrategy(int window, int shortWindow)
        {
            _window = window;
            _shortWindow = shortWindow;
        }

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
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
            var fractal = FractalEfficiency(shortBars);


            var minMid = _bars.Min(x => x.Mid);
            var maxMid = _bars.Max(x => x.Mid);

            var currentBar = bar;

            var fractalLimit = 0.90;

            if (hasPosition)
            {
                _currentPositionExitTimeSpan += currentBar.TimestampDiffMs;

                if (_currentPositionExitTimeSpan >= _currentPositionEntryTimeSpan)
                {
                    if (inventoryAbsolute > 0)
                        return Action.Sell;
                    if (inventoryAbsolute < 0)
                        return Action.Buy;
                }
            }

            if (currentBar.Mid >= maxMid && fractal > fractalLimit)
            {
                _currentPositionEntryTimeSpan = shortBars.Sum(x => x.TimestampDiffMs);
                _currentPositionExitTimeSpan = 0;
                return Action.Buy;
            }

            if (currentBar.Mid <= minMid && fractal > fractalLimit)
            {
                _currentPositionEntryTimeSpan = shortBars.Sum(x => x.TimestampDiffMs);
                _currentPositionExitTimeSpan = 0;
                return Action.Sell;
            }

            return Action.Nothing;
        }

        private double FractalEfficiency(RangeBarModel[] bars)
        {
            var count = bars.Length;
            var absPath = Math.Abs(bars[count - 1].Mid - bars[0].Mid);
            var diff = ComputeDiff(bars);
            var sumPath = diff.Select(Math.Abs).Sum();
            //return Math.Round(absPath / sumPath, 4);
            return absPath / sumPath;
        }

        private IEnumerable<double> ComputeDiff(RangeBarModel[] bars)
        {
            for (int i = bars.Length-1; i > 0; i--)
            {
                var current = bars[i];
                var before = bars[i - 1];
                yield return current.Mid - before.Mid;
            }
        }


        /*
         *
            def fractal_efficiency(array) -> float:
                abs_path = abs(array[-1] - array[0])
                sum_path = np.sum(np.abs(np.diff(array)))
                fractal_efficiency = round(abs_path / sum_path, 4)
                return fractal_efficiency
         *
         */
    }
}
