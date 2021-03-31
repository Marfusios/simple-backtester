using System.IO;
using Microsoft.ML;
using SimpleBacktester.Data;

namespace SimpleBacktester.Strategies
{
    public class MlStrategy : ITakerStrategy
    {
        private readonly PredictionEngine<RangeBar, RangeBarPrediction> _predictionEngine;
        private RangeBarModel _prevBar;

        public MlStrategy(string range)
        {
            //Create ML Context with seed for repeteable/deterministic results
            var mlContext = new MLContext(seed: 0);

            //var modelPath = $"C:\\dev\\ml\\RangeBarModel{range}.zip";
            var modelPath = $"C:\\dev\\ml\\RangeBarModel_All_Auto{range}.zip";

            ITransformer trainedModel;
            using (var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                trainedModel = mlContext.Model.Load(stream, out var modelInputSchema);
            }

            // Create prediction engine related to the loaded trained model
            _predictionEngine = mlContext.Model.CreatePredictionEngine<RangeBar, RangeBarPrediction>(trainedModel);
        }

        public Action Decide(RangeBarModel bar, double inventoryAbsolute)
        {
            if (bar.TimestampDiffMs < (1000 * 60 * 1))
            {
                // to fast bar, not executable
                return Action.Nothing;
            }

            var convertedBar = new RangeBar();
            convertedBar.VolumeRate = ComputeRate(bar.BuyVolume, bar.SellVolume);
            convertedBar.CountRate = ComputeRate(bar.BuyCount, bar.SellCount);
            convertedBar.ChangeRate = ComputeRate(bar.PriceChangedUpCount, bar.PriceChangedDownCount);
            convertedBar.InsertedRate = ComputeRate(bar.ObInsertedCountBid, bar.ObInsertedCountAsk);
            convertedBar.UpdatedRate = ComputeRate(bar.ObUpdatedCountBid, bar.ObUpdatedCountAsk);
            convertedBar.DeletedRate = ComputeRate(bar.ObDeletedCountBid, bar.ObDeletedCountAsk);

            if (_prevBar != null)
            {
                convertedBar.MidPrev = (float) _prevBar.CurrentPrice;
                convertedBar.MidChangePrev = (float) (bar.CurrentPrice - _prevBar.CurrentPrice);
            }

            var prediction = _predictionEngine.Predict(convertedBar);

            //if (Math.Abs(prediction.Score) < 0.5)
            //    return Action.Nothing;

            _prevBar = bar;

            if (!string.IsNullOrWhiteSpace(prediction.Prediction))
            {   
                if (prediction.Prediction == "1")
                    return Action.Sell;
                return Action.Buy;
            }

            if (prediction.Direction)
                return Action.Buy;
            return Action.Sell;
        }

        private static float ComputeRate(double buy, double sell)
        {
            return (float)((buy - sell) / (buy + sell));
        }
    }
}
