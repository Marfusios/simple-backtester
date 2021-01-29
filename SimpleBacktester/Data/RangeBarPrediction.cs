using System;
using Microsoft.ML.Data;

namespace SimpleBacktester.Data
{
    public class RangeBarPrediction
    {
        //[ColumnName("Score")]
        //public float MidNext;

        [ColumnName("Probability")]
        public float Probability;

        //[ColumnName("PredictedLabel")]
        public bool Direction;

        [ColumnName("Score")]
        public float[] Score;


        [ColumnName("PredictedLabel")]
        public String Prediction { get; set; }

        //[ColumnName("Score")]
        //public float[] Score;
    }
}
