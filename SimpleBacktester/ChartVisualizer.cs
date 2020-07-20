using System;
using System.IO;
using System.Linq;
using System.Threading;
using PLplot;
using SimpleBacktester.Data;

namespace SimpleBacktester
{
    public class ChartVisualizer
    {
        public void Plot(string name, string nameWithFee, string filename, int totalBars, 
            RangeBarModel[] prices, TradeModel[] orders, 
            ProfitInfo[] days, ProfitInfo[] months)
        {
            if (!prices.Any())
                return;

            var filenameSafe = CheckFilename(filename);

            // ISSUE: "Cannot find support PLplot support files in System.String[]."
            // SOLUTION: copy 'runtimes' directory
            //     from ==> M:\ProjectFolder\bin\Debug\netcoreapp3.1\runtimes
            //     to   ==> M:\ProjectFolder\bin\runtimes
            var pl = InitPlot(filenameSafe);

            
            pl.star(2, 3);

            PlotTrades($"trades (by bar)", totalBars, prices, orders, false, pl);
            PlotTrades("trades (by time)", totalBars, prices, orders, true, pl);

            if (!PlotPnl($"Pnl (total) {name}", "Days", days, true, false, pl))
                PlotPnl($"Pnl (total) {name} ", "Months", months, true, false, pl);

            if (!PlotPnl("Pnl (per day)", "Days", days, false, false, pl))
                PlotPnl("Pnl (per months)", "Months", months, false, false, pl);

            if (!PlotPnl($"Pnl with fee (total) {nameWithFee}", "Days", days, true, true, pl))
                PlotPnl($"Pnl with fee (total) {nameWithFee}", "Months", months, true, true, pl);

            if (!PlotPnl("Pnl with fee (per day)", "Days", days, false, true, pl))
                PlotPnl("Pnl with fee (per months)", "Months", months, false, true, pl);

            // end page (writes output to disk)
            pl.eop();

            Thread.Sleep(500);
            ((IDisposable)pl).Dispose();
        }

        private static void PlotTrades(string name, int totalBars, RangeBarModel[] prices, TradeModel[] orders, bool byTime,
            PLStream pl)
        {
            var priceCount = prices.Length;
            var priceX = new double[priceCount];
            var priceY = new double[priceCount];
            var priceTimestamp = new double[priceCount];

            for (var j = 0; j < priceCount; j++)
            {
                var price = prices[j];

                priceTimestamp[j] = price.Timestamp;
                priceX[j] = byTime ? price.Timestamp : j;
                priceY[j] = price.CurrentPrice;
            }

            var buys = orders
                .Where(x => x.Amount > 0)
                .OrderBy(x => x.Timestamp)
                .ToArray();
            var sells = orders
                .Where(x => x.Amount < 0)
                .OrderBy(x => x.Timestamp)
                .ToArray();

            var buysCount = buys.Length;
            var buyX = new double[buysCount];
            var buyY = new double[buysCount];

            for (var j = 0; j < buysCount; j++)
            {
                var order = buys[j];
                var priceIndex = Array.IndexOf(priceTimestamp, order.Timestamp);

                buyX[j] = byTime ? order.Timestamp : priceIndex;
                buyY[j] = Math.Abs(order.Price);
            }

            var sellsCount = sells.Length;
            var sellX = new double[sellsCount];
            var sellY = new double[sellsCount];

            for (var j = 0; j < sellsCount; j++)
            {
                var order = sells[j];
                var priceIndex = Array.IndexOf(priceTimestamp, order.Timestamp);

                sellX[j] = byTime ? order.Timestamp : priceIndex;
                sellY[j] = Math.Abs(order.Price);
            }


            // set axis limits
            var xMin = byTime ? prices.First().Timestamp - 10000 : 0;
            var xMax = byTime ? prices.Last().Timestamp + 10000 : prices.Length + 1;
            var yMin = prices.Min(x => x.CurrentPrice);
            var yMax = prices.Max(x => x.CurrentPrice);

            yMin = yMin - (yMin * 0.001);
            yMax = yMax + (yMax * 0.001);

            pl.col0(8);

            // Set scaling for mail title text 125% size of default
            //pl.schr(0, 1.25);

            pl.env(xMin, xMax, yMin, yMax, AxesScale.Independent, AxisBox.BoxTicksLabelsAxes);

            // The main title
            if (byTime)
                pl.lab($"Timestamp (to {prices.LastOrDefault()?.TimestampDate:d})", "Price", name);
            else
                pl.lab($"Bars (first {prices.Length} of {totalBars})", "Price", name);
            //pl.timefmt("%s");

            // plot using different colors
            // see http://plplot.sourceforge.net/examples.php?demo=02 for palette indices

            pl.col0(3);
            pl.width(0.4);

            //pl.poin(buyX, buyY, (char)8);
            for (int i = 0; i < buyX.Length; i++)
            {
                pl.line(new[]
                {
                    buyX[i], buyX[i],
                }, new[]
                {
                    yMin, buyY[i]
                });
            }

            pl.col0(14);
            //pl.poin(sellX, sellY, (char)9);
            for (int j = 0; j < sellX.Length; j++)
            {
                pl.line(new[]
                {
                    sellX[j], sellX[j],
                }, new[]
                {
                    sellY[j], yMax,
                });
            }


            pl.col0(7);
            pl.width(1);
            pl.line(priceX, priceY);
        }

        private bool PlotPnl(string name, string xField, ProfitInfo[] items, bool total, bool withFee, PLStream pl)
        {

            var itemCount = items.Length;
            if (itemCount <= 0)
                return false;

            var itemX = new double[itemCount];
            var itemY = new double[itemCount];
            var totalPnl = 0.0;

            for (var j = 0; j < itemCount; j++)
            {
                var item = items[j];
                totalPnl += withFee ? item.PnlWithFee : item.Pnl;

                itemX[j] = j;
                itemY[j] = total ? totalPnl : (withFee ? item.PnlWithFee : item.Pnl);
            }

            // set axis limits
            var xMin = 0;
            var xMax = itemX.Length + 1;
            var yMin = itemY.Min();
            var yMax = itemY.Max();

            yMin = yMin - (Math.Abs(yMin * 0.1));
            yMax = yMax + (Math.Abs(yMax * 0.1));

            if (Math.Abs(yMin - yMax) <= 0e8)
            {
                // invalid scale, do not draw chart
                return false;
            }

            pl.col0(8);

            // Set scaling for mail title text 125% size of default
            //pl.schr(0, 1.25);

            pl.env(xMin, xMax, yMin, yMax, AxesScale.Independent, AxisBox.BoxTicksLabelsAxes);
            pl.lab(xField, "Profit", name);

            pl.col0(12);
            pl.line(itemX, itemY);

            return true;
        }


        private static string CheckFilename(string filename)
        {
            var filenameSafe = (filename ?? string.Empty).ToLower();

            if (string.IsNullOrWhiteSpace(filenameSafe))
            {
                filenameSafe = "chart.svg";
            }

            if (!filenameSafe.EndsWith("svg"))
            {
                filenameSafe = $"{filenameSafe}.svg";
            }

            return Path.GetFullPath(filenameSafe);
        }


        private static PLStream InitPlot(string filenameSafe)
        {
            // create PLplot object
            var pl = new PLStream();

            pl.sdev("svg");
            pl.sfnam(filenameSafe);

            //pl.sdev("pngcairo");
            //pl.sfnam("aaaa.png");

            // use white background with black foreground
            //pl.spal0("cmap0_alternate.pal");

            // Initialize plplot
            //pl.init();
            return pl;
        }

        private static double ConvertTimestamp(DateTime time)
        {
            return ConvertToUnixTime(time);
            //return double.Parse($"1{time.Hour:00}{time.Minute:00}{time.Second:00}{time.Millisecond:000}");
        }

        private static double ConvertTimestamp(DateTime? time)
        {
            return ConvertToUnixTime(time ?? DateTime.MaxValue);
        }

        public static readonly DateTime UnixBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static double ConvertToUnixTime(DateTime time)
        {
            return time.Subtract(UnixBase).TotalMilliseconds;
        }
    }
}
