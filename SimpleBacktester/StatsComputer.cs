using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleBacktester
{
    public class StatsComputer
    {
        public static double ComputeProfitNoExcess(TradeModel[] executedOrders, double fee)
        {
            double diff = 0;
            var bids = executedOrders.Where(x => x.Amount > 0).OrderBy(x => x.Timestamp).ToArray();
            var asks = executedOrders.Where(x => x.Amount < 0).OrderBy(x => x.Timestamp).ToArray();

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
            var bids = executedOrders.Where(x => x.Amount > 0).OrderBy(x => x.Timestamp).ToArray();
            var asks = executedOrders.Where(x => x.Amount < 0).OrderBy(x => x.Timestamp).ToArray();

            var totalBidAmount = bids.Sum(x => Math.Abs(x.Amount));
            var totalAskAmount = asks.Sum(x => Math.Abs(x.Amount));
            var excessAmount = totalBidAmount - totalAskAmount;

            var totalBidQuote = bids.Sum(x => Math.Abs(x.Amount * x.Price));
            var totalAskQuote = asks.Sum(x => Math.Abs(x.Amount * x.Price));

            var result = new ProfitInfo()
            {
                Pnl = 0,
                TradesCount = executedOrders.Length,
                BuysCount = bids.Length,
                SellsCount = asks.Length,
                TotalBought = totalBidAmount,
                TotalSold = totalAskAmount,
                TotalBoughtQuote = totalBidQuote,
                TotalSoldQuote = totalAskQuote,
                AverageBuyPrice = ComputeAveragePrice(bids),
                AverageSellPrice = ComputeAveragePrice(asks),
                WinRate = ComputeWinRate(executedOrders),
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
            result.PnlNoExcess = ComputeProfitNoExcess(executedOrders, fee);
            result.Pnl = totalDiff;
            return result;
        }

        private static double ComputeWinRate(TradeModel[] executedOrders)
        {
            var orders = executedOrders.OrderBy(x => x.Timestamp).ToArray();
            var totalPositions = 0.0;
            var winPositions = 0.0;

            var stack = new Stack<TradeModel>();

            foreach (var currentOrder in orders)
            {
                if (currentOrder.PositionState == PositionState.Close)
                {
                    var count = stack.Count;
                    totalPositions += count;

                    foreach (var previousOrder in stack)
                    {
                        var isBuy = previousOrder.Amount >= 0;
                        var isWin = isBuy ? previousOrder.Price < currentOrder.Price : previousOrder.Price > currentOrder.Price;
                        winPositions += isWin ? 1 : 0;
                    }

                    stack.Clear();
                    continue;
                }

                stack.Push(currentOrder);
            }

            var winRate = winPositions / totalPositions;
            return winRate;
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
