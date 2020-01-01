using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RangeBarProfit
{
    public class StatsComputer
    {
        public static double ComputeProfitNoExcess(TradeModel[] executedOrders, double fee)
        {
            double diff = 0;
            var bids = executedOrders.Where(x => x.Amount > 0).ToArray();
            var asks = executedOrders.Where(x => x.Amount < 0).ToArray();

            var bidAmount = bids.Sum(x => x.Amount);
            var askAmount = asks.Sum(x => Math.Abs(x.Amount));
            var minAmount = Math.Min(bidAmount, askAmount);

            var amountForBid = minAmount;
            foreach (var order in bids)
            {
                var amount = order.Amount ;
                var price = order.Price;
                amountForBid -= amount;
                if (amountForBid >= 0)
                {
                    diff -= ComputeOrderValue(amount, price, fee, true);
                }
                else
                {
                    var amountTmp = amount + amountForBid;
                    diff -= ComputeOrderValue(amountTmp, price, fee, true);
                    break;
                }
            }

            var amountForSell = minAmount;
            foreach (var order in asks)
            {
                var amount = Math.Abs(order.Amount);
                var price = order.Price;
                amountForSell -= amount;
                if (amountForSell >= 0)
                {
                    diff += ComputeOrderValue(amount, price, fee, false);
                }
                else
                {
                    var amountTmp = amount + amountForSell;
                    diff += ComputeOrderValue(amountTmp, price, fee, false);
                    break;
                }
            }

            return diff;
        }

        public static ProfitInfo ComputeProfitComplex(TradeModel[] executedOrders, double fee)
        {
            double diff = 0;
            var bids = executedOrders.Where(x => x.Amount > 0).ToArray();
            var asks = executedOrders.Where(x => x.Amount < 0).ToArray();

            var totalBidAmount = bids.Sum(x => Math.Abs(x.Amount));
            var totalAskAmount = asks.Sum(x => Math.Abs(x.Amount));
            var excessAmount = totalBidAmount - totalAskAmount;

            var result = new ProfitInfo()
            {
                Pnl = 0,
                TradesCount = executedOrders.Length,
                BuysCount = bids.Length,
                SellsCount = asks.Length,
                TotalBought = totalBidAmount,
                TotalSold = totalAskAmount,
                AverageBuy = ComputeAveragePrice(bids),
                AverageSell = ComputeAveragePrice(asks),
                ExcessAmount = excessAmount
            };

            if (!bids.Any() || !asks.Any())
            {
                return result;
            }

            var amountForBid = totalBidAmount;
            foreach (var bidTrade in bids)
            {
                var amount = bidTrade.Amount;
                var price = bidTrade.Price;
                amountForBid -= amount;
                if (amountForBid >= 0)
                {
                    diff -= ComputeOrderValue(amount, price, fee, true);
                }
                else
                {
                    var amountTmp = amount + amountForBid;
                    diff -= ComputeOrderValue(amountTmp, price, fee, true);
                    break;
                }
            }

            var amountForSell = totalAskAmount;
            foreach (var askTrade in asks)
            {
                var amount = Math.Abs(askTrade.Amount);
                var price = askTrade.Price;
                amountForSell -= amount;
                if (amountForSell >= 0)
                {
                    diff += ComputeOrderValue(amount, price, fee, false);
                }
                else
                {
                    var amountTmp = amount + amountForSell;
                    diff += ComputeOrderValue(amountTmp, price, fee, false);
                    break;
                }
            }

            double lastTrade;
            if (excessAmount > 0)
            {
                var lastPrice = bids.LastOrDefault()?.Price ?? 0;
                lastTrade = excessAmount * lastPrice;
            }
            else
            {
                var lastPrice = asks.LastOrDefault()?.Price ?? 0;
                lastTrade = excessAmount * lastPrice;
            }

            var totalDiff = diff + lastTrade;
            result.Pnl = totalDiff;
            result.PnlNoExcess = ComputeProfitNoExcess(executedOrders, fee);
            return result;
        }

        public static double ComputeAveragePrice(TradeModel[] orders)
        {
            if (orders == null || !orders.Any())
                return 0;

            var sum = orders.Sum(x => x.Amount);
            var total = orders.Sum(x => x.Price * x.Amount);
            return total / sum;
        }

        private static double ComputeOrderValue(double amount, double price, double fee, bool isBid)
        {
            // rebate: (10 * 10100 + (10 * 10100 * 0.00025)) - (10 * 10000 - (10 * 10000 * 0.00025))
            // fee:    (10 * 10100 - (10 * 10100 * 0.001)) - (10 * 10000 + (10 * 10000 * 0.001))

            var orderValue = Math.Abs(amount) * Math.Abs(price);
            var feeOrRebate = orderValue * fee;
            return isBid ?
                orderValue + feeOrRebate :
                orderValue - feeOrRebate;
        }
    }
}
