using System;
using System.Diagnostics;
using CsvHelper.Configuration.Attributes;

namespace SimpleBacktester.Data
{
    [DebuggerDisplay("Bar {Index} {TimestampDate} {CurrentPrice}")]
    public class RangeBarModel
    {
        private DateTime? _date;

        [Name("timestamp_unix")]
        public double TimestampUnix { get; set; }

        public DateTime? Timestamp
        {
            get => _date;
            set
            {
                if (value == null)
                    return;

                _date = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
                TimestampUnix = _date.Value.ToUnixSeconds();
            }
        }

        public DateTime? Date
        {
            get => _date;
            set
            {
                if (value == null)
                    return;

                _date = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
                TimestampUnix = _date.Value.ToUnixSeconds();
            }
        }

        public double? Mid => (Bid + Ask) * 0.5;

        /// <summary>
        /// Used for TAKER strategy (SELL order)
        /// </summary>
        public double? Bid { get; set; }

        /// <summary>
        /// Used for TAKER strategy (BUY order)
        /// </summary>
        public double? Ask { get; set; }

        public double? Open { get; set; }

        /// <summary>
        /// Used for MAKER strategy (ASK order) when 'HighBuy' is not available
        /// </summary>
        public double? High { get; set; }

        /// <summary>
        /// Used for MAKER strategy (BID order) when 'LowSell' is not available
        /// </summary>
        public double? Low { get; set; }
        public double? Close { get; set; }


        /// <summary>
        /// Used for MAKER strategy (ASK order)
        /// </summary>
        [Name("high_buy")]
        public double? HighBuy { get; set; }

        /// <summary>
        /// Used for MAKER strategy (BID order)
        /// </summary>
        [Name("low_sell")]
        public double? LowSell { get; set; }


        public double? Volume { get; set; }


        /// <summary>
        /// Used as fallback for TAKER strategy as execution price instead of 'Bid' or 'Ask'
        /// </summary>
        [Ignore] 
        public double CurrentPrice => Close ?? Mid ?? 0;

        [Ignore]
        public double InitialPrice => Open ?? Mid ?? 0;

        [Ignore]
        public int Index { get; set; }

        [Ignore]
        public DateTime TimestampDate => Timestamp ?? DateUtils.ConvertToTimeFromSec(TimestampUnix);







        // TODO: make generic
        [Name("timestamp_diff_ms")]
        public double TimestampDiffMs { get; set; }

        [Name("order_book_inserted_bid_count")]
        public int ObInsertedCountBid { get; set; }

        [Name("order_book_inserted_ask_count")]
        public int ObInsertedCountAsk { get; set; }


        [Name("order_book_updated_bid_count")]
        public int ObUpdatedCountBid { get; set; }

        [Name("order_book_updated_ask_count")]
        public int ObUpdatedCountAsk { get; set; }


        [Name("order_book_deleted_bid_count")]
        public int ObDeletedCountBid { get; set; }

        [Name("order_book_deleted_ask_count")]
        public int ObDeletedCountAsk { get; set; }


        [Name("buy_volume")]
        public double BuyVolume { get; set; }

        [Name("sell_volume")]
        public double SellVolume { get; set; }


        [Name("price_changed_up_count")]
        public double PriceChangedUpCount { get; set; }

        [Name("price_changed_down_count")]
        public double PriceChangedDownCount { get; set; }


        [Name("buy_count")]
        public double BuyCount { get; set; }

        [Name("sell_count")]
        public double SellCount { get; set; }


        [Name("mid_change")]
        public double? MidChange { get; set; }




        [Name("ob_liquidity_bid")]
        public double? ObLiquidityBid { get; set; }

        [Name("ob_liquidity_ask")]
        public double? ObLiquidityAsk { get; set; }

    }
}
