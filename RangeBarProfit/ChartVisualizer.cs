﻿using System;
using System.Linq;
using System.Threading;
using PLplot;

namespace RangeBarProfit
{
    public class ChartVisualizer
    {
        public void PlotTrades(string name, string filename, RangeBarModel[] prices, TradeModel[] orders, bool byTime)
        {
            var filenameSafe = CheckFilename(filename);

            var priceCount = prices.Length;
            var priceX = new double[priceCount];
            var priceY = new double[priceCount];
            var priceTimestamp = new double[priceCount];

            for (var j = 0; j < priceCount; j++)
            {
                var price = prices[j];

                priceTimestamp[j] = price.Timestamp;
                priceX[j] = byTime ? price.Timestamp : j;
                priceY[j] = price.Mid;
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

            var pl = InitPlot(filenameSafe);

            // set axis limits
            var xMin = byTime ? prices.First().Timestamp - 10000 : 0;
            var xMax = byTime ? prices.Last().Timestamp + 10000 : prices.Length+1;
            var yMin = prices.Min(x => x.Mid);
            var yMax = prices.Max(x => x.Mid);

            yMin = yMin - (yMin * 0.001);
            yMax = yMax + (yMax * 0.001);

            pl.env(xMin, xMax, yMin, yMax, AxesScale.Independent, AxisBox.BoxTicksLabelsAxes);
            // pl.vpor(0, 960, 0, 720);
            // pl.wind(0, 960, 0, 720);

            // Set scaling for mail title text 125% size of default
            // pl.schr(0, 1.25);

            // The main title
            if(byTime)
                pl.lab("Timestamp", "Price", name);
            else
                pl.lab("Step", "Price", name);
            //pl.timefmt("%s");

            // plot using different colors
            // see http://plplot.sourceforge.net/examples.php?demo=02 for palette indices

            pl.col0(3);
            //pl.poin(buyX, buyY, (char)8);
            for (int i = 0; i < buyX.Length; i++)
            {
                pl.line(new[]
                {
                    buyX[i],  buyX[i],
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
                    sellX[j],  sellX[j],
                }, new[]
                {
                    sellY[j], yMax,
                });
            }


            pl.col0(7);
            pl.line(priceX, priceY);

            // end page (writes output to disk)
            pl.eop();

            Thread.Sleep(500);
            ((IDisposable)pl).Dispose();
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

            return filenameSafe;
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
            pl.init();
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