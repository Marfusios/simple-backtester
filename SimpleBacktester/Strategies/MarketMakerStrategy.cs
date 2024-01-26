using System;
using System.Collections.Generic;
using System.Linq;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class MarketMakerStrategy : IMakerStrategy
    {
        private readonly int _barsMemory;
        private readonly double _spreadDec;
        private readonly double _spreadMin;

        private int _barCounter = -1;
        private double? _lastPrice;
        private double? _lastRatio;

        private readonly Queue<RangeBarModel> _lastBars = new Queue<RangeBarModel>();


        public MarketMakerStrategy(int barsMemory, double spreadDec, double spreadMin)
        {
            _barsMemory = barsMemory;
            _spreadDec = spreadDec;
            _spreadMin = spreadMin;
        }

        public PlacedOrder[] Decide(RangeBarModel bar, double inventoryAbsolute, PlacedOrder[] placedOrders)
        {
            _barCounter++;
            //if (_skipBars > 0 && _barCounter % _skipBars != 0)
            //    return placedOrders;

            if (_lastPrice == null)
            {
                _lastPrice = bar.CurrentPrice;
                return placedOrders;
            }

            var liquidityRatio = (bar.ObLiquidityBid - bar.ObLiquidityAsk) / (bar.ObLiquidityBid + bar.ObLiquidityAsk);
            var ratioDiff = liquidityRatio - _lastRatio;
            var ratioDiffPer = ratioDiff * 100;
            _lastRatio = liquidityRatio;

            var ratioDiffPerAbs = 0; //Math.Abs(ratioDiffPer ?? 0);
            // var ratioSigmoid = Sigmoid(ratioDiffPer ?? 0);

            var obBidDiff = Sigmoid((bar.ObUpdatedVolumeDiffBid / bar.ObLiquidityBid ?? 0) * 100) - 0.5;
            var obAskDiff = Sigmoid((bar.ObUpdatedVolumeDiffAsk / bar.ObLiquidityAsk ?? 0) * 100) - 0.5;

            var obBidDeleteModifier = GetVolumeModifier(bar.ObDeletedVolumeBid);
            var obAskDeleteModifier = GetVolumeModifier(bar.ObDeletedVolumeAsk);

            _lastBars.Enqueue(bar);
            if (_lastBars.Count > _barsMemory)
            {
                _lastBars.Dequeue();
            }
            else if (_lastBars.Count < _barsMemory)
            {
                _lastPrice = bar.CurrentPrice;
                return placedOrders;
            }

            var bidModifier = GetVolumeModifier(_lastBars.Sum(x => x.ObDeletedVolumeBid));
            var askModifier = GetVolumeModifier(_lastBars.Sum(x => x.ObDeletedVolumeAsk));

            var bought = _lastBars.Sum(x => x.BuyVolume ?? 0);
            var sold = _lastBars.Sum(x => x.SellVolume ?? 0);

            var boughSig = Sigmoid(bought) - 0.5;
            var soldSig = Sigmoid(sold) - 0.5;

            //var targetSpreadBidPer = Math.Max(soldSig + ratioDiffPerAbs + obAskDiff + _spreadDec, _spreadMin);
            //var targetSpreadAskPer = Math.Max(boughSig + ratioDiffPerAbs + obBidDiff + _spreadDec, _spreadMin);

            var targetSpreadBidPer = Math.Max(bidModifier, _spreadMin);
            var targetSpreadAskPer = Math.Max(askModifier, _spreadMin);

            var bid = (bar.Bid ?? bar.CurrentPrice);
            var targetSpreadBid = targetSpreadBidPer / 100;
            var targetPriceBidDiff = Math.Abs(bid * targetSpreadBid);
            var targetPriceBid = bid - targetPriceBidDiff;

            var ask = (bar.Ask ?? bar.CurrentPrice);
            var targetSpreadAsk = targetSpreadAskPer / 100;
            var targetPriceAskDiff = Math.Abs(ask * targetSpreadAsk);
            var targetPriceAsk = ask + targetPriceAskDiff;

            _lastPrice = bar.CurrentPrice;

            return new[]
            {
                new PlacedOrder(OrderSide.Bid, targetPriceBid, inventoryAbsolute >= 0 ? (double?)null : inventoryAbsolute),
                new PlacedOrder(OrderSide.Ask, targetPriceAsk, inventoryAbsolute <= 0 ? (double?)null : inventoryAbsolute)
            };
        }

        private double GetVolumeModifier(double? volume)
        {
            var modifier = _spreadMin;

            if (volume >= 80000)
                return modifier * 6;

            if (volume >= 40000)
                return modifier * 5;

            if (volume >= 20000)
                return modifier * 4;

            if (volume >= 10000)
                return modifier * 3;

            if (volume >= 5000)
                return modifier * 2;

            return 0;
        }

        public static double Sigmoid(double value)
        {
            return 1.0f / (1.0f + Math.Exp(-value));
        }
    }
}
