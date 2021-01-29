using CsvHelper.Configuration.Attributes;
using Microsoft.ML.Data;

namespace SimpleBacktester.Data
{
      public class RangeBar
    {
        [Name("mid")]
        [ColumnName("mid")]
        public float Mid { get; set; }

        [Name("open")]
        [ColumnName("open")]
        public float Open { get; set; }

        [Name("close")]
        [ColumnName("close")]
        public float Close { get; set; }

        [Name("high")]
        [ColumnName("high")]
        public float High { get; set; }

        [Name("low")]
        [ColumnName("low")]
        public float Low { get; set; }

        [Name("timestamp")]
        [ColumnName("timestamp")]
        public float Timestamp { get; set; }

        [Name("timestamp_diff_ms")]
        [ColumnName("timestamp_diff_ms")]
        public float TimestampDiffMs { get; set; }

        [Name("count")]
        [ColumnName("count")]
        public float Count { get; set; }

        [Name("buy_count")]
        [ColumnName("buy_count")]
        public float BuyCount { get; set; }

        [Name("buy_count_agg")]
        [ColumnName("buy_count_agg")]
        public float BuyCountAgg { get; set; }

        [Name("sell_count")]
        [ColumnName("sell_count")]
        public float SellCount { get; set; }

        [Name("sell_count_agg")]
        [ColumnName("sell_count_agg")]
        public float SellCountAgg { get; set; }


        [Name("price_changed_count")]
        [ColumnName("price_changed_count")]
        public float PriceChangedCount { get; set; }

        [Name("price_changed_up_count")]
        [ColumnName("price_changed_up_count")]
        public float PriceChangedUpCount { get; set; }

        [Name("price_changed_up_count_agg")]
        [ColumnName("price_changed_up_count_agg")]
        public float PriceChangedUpCountAgg { get; set; }

        [Name("price_changed_down_count")]
        [ColumnName("price_changed_down_count")]
        public float PriceChangedDownCount { get; set; }

        [Name("price_changed_down_count_agg")]
        [ColumnName("price_changed_down_count_agg")]
        public float PriceChangedDownCountAgg { get; set; }


        [Name("order_book_changed_count")]
        [ColumnName("order_book_changed_count")]
        public float OrderBookChangedCount { get; set; }


        [Name("order_book_inserted_count")]
        [ColumnName("order_book_inserted_count")]
        public float OrderBookInsertedCount { get; set; }

        [Name("order_book_inserted_bid_count")]
        [ColumnName("order_book_inserted_bid_count")]
        public float OrderBookInsertedBidCount { get; set; }

        [Name("order_book_inserted_bid_count_agg")]
        [ColumnName("order_book_inserted_bid_count_agg")]
        public float OrderBookInsertedBidCountAgg { get; set; }

        [Name("order_book_inserted_ask_count")]
        [ColumnName("order_book_inserted_ask_count")]
        public float OrderBookInsertedAskCount { get; set; }

        [Name("order_book_inserted_ask_count_agg")]
        [ColumnName("order_book_inserted_ask_count_agg")]
        public float OrderBookInsertedAskCountAgg { get; set; }


        [Name("order_book_updated_count")]
        [ColumnName("order_book_updated_count")]
        public float OrderBookUpdatedCount { get; set; }

        [Name("order_book_updated_bid_count")]
        [ColumnName("order_book_updated_bid_count")]
        public float OrderBookUpdatedBidCount { get; set; }

        [Name("order_book_updated_bid_count_agg")]
        [ColumnName("order_book_updated_bid_count_agg")]
        public float OrderBookUpdatedBidCountAgg { get; set; }

        [Name("order_book_updated_ask_count")]
        [ColumnName("order_book_updated_ask_count")]
        public float OrderBookUpdatedAskCount { get; set; }

        [Name("order_book_updated_ask_count_agg")]
        [ColumnName("order_book_updated_ask_count_agg")]
        public float OrderBookUpdatedAskCountAgg { get; set; }


        [Name("order_book_deleted_count")]
        [ColumnName("order_book_deleted_count")]
        public float OrderBookDeletedCount { get; set; }

        [Name("order_book_deleted_bid_count")]
        [ColumnName("order_book_deleted_bid_count")]
        public float OrderBookDeletedBidCount { get; set; }

        [Name("order_book_deleted_bid_count_agg")]
        [ColumnName("order_book_deleted_bid_count_agg")]
        public float OrderBookDeletedBidCountAgg { get; set; }

        [Name("order_book_deleted_ask_count")]
        [ColumnName("order_book_deleted_ask_count")]
        public float OrderBookDeletedAskCount { get; set; }

        [Name("order_book_deleted_ask_count_agg")]
        [ColumnName("order_book_deleted_ask_count_agg")]
        public float OrderBookDeletedAskCountAgg { get; set; }



        [Name("volume")]
        [ColumnName("volume")]
        public float Volume { get; set; }

        [Name("buy_volume")]
        [ColumnName("buy_volume")]
        public float BuyVolume { get; set; }

        [Name("buy_volume_agg")]
        [ColumnName("buy_volume_agg")]
        public float BuyVolumeAgg { get; set; }

        [Name("sell_volume")]
        [ColumnName("sell_volume")]
        public float SellVolume { get; set; }

        [Name("sell_volume_agg")]
        [ColumnName("sell_volume_agg")]
        public float SellVolumeAgg { get; set; }


        [Name("mid_change")]
        [ColumnName("mid_change")]
        public float MidChange { get; set; }

        [Name("mid_change_agg")]
        [ColumnName("mid_change_agg")]
        public float MidChangeAgg { get; set; }


        [Name("mid_change_unmodified")]
        [ColumnName("mid_change_unmodified")]
        public float MidChangeUnmodified { get; set; }

        [ColumnName("direction")] 
        public bool Direction => MidChangeUnmodified >= 0;

        [ColumnName("mid_prev")]
        public float MidPrev { get; set; }

        [ColumnName("mid_change_prev")]
        public float MidChangePrev { get; set; }

        [ColumnName("mid_next")]
        public float MidNext { get; set; }

        [ColumnName("mid_change_next")]
        public float MidChangeNext { get; set; }

        [ColumnName("Probability")]
        public float Probability;

        [ColumnName("direction_next")]
        public bool DirectionNext => MidChangeNext >= 0;

        [ColumnName("direction_next_integer")]
        public string DirectionNextInteger { get; set; }


        // RATES

        [ColumnName("vol_rate")]
        public float VolumeRate { get; set; }

        [ColumnName("count_rate")]
        public float CountRate { get; set; }

        [ColumnName("change_rate")]
        public float ChangeRate { get; set; }

        [ColumnName("inserted_rate")]
        public float InsertedRate { get; set; }

        [ColumnName("updated_rate")]
        public float UpdatedRate { get; set; }

        [ColumnName("deleted_rate")]
        public float DeletedRate { get; set; }
    }
}
