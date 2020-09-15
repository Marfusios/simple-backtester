using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingViewUdfProvider;
using TradingViewUdfProvider.Models;

namespace SimpleBacktester.Visualization
{
    public class MyTvProvider : ITradingViewProvider
    {
        public static readonly List<TvSymbolInfo> Symbols = new List<TvSymbolInfo>();
        public static readonly Dictionary<string, TvBar[]> Bars =
            new Dictionary<string, TvBar[]>(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<string, TvMark[]> Marks =
            new Dictionary<string, TvMark[]>(StringComparer.OrdinalIgnoreCase);

        public Task<TvConfiguration> GetConfiguration()
        {
            var config = new TvConfiguration
            {
                SupportedResolutions = new[] {"1S","30S","1","60","120","240","D","2D","3D","W","3W","M","6M"},
                SupportGroupRequest = false,
                SupportMarks = true,
                SupportSearch = true,
                SupportTimeScaleMarks = false
            };
            return Task.FromResult(config);
        }

        public async Task<TvSymbolInfo> GetSymbol(string symbol)
        {
            await Task.Yield();

            var symbolSafe = (symbol ?? string.Empty);
            if (symbolSafe.Contains(":"))
            {
                var split = symbolSafe.Split(":");
                symbolSafe = split.LastOrDefault() ?? symbolSafe;
            }

            return Symbols
                .FirstOrDefault(x => x.Ticker.Equals(symbolSafe, StringComparison.OrdinalIgnoreCase));
        }

        public Task<TvSymbolSearch[]> FindSymbols(string query, string type, string exchange, int? limit)
        {
            var querySafe = query ?? string.Empty;
            var symbols = Symbols
                .Select(x => new TvSymbolSearch()
                {
                    Symbol = x.Name,
                    Ticker = x.Ticker,
                    Description = x.Description,
                    Exchange = x.ExchangeListed,
                    Type = x.Type,
                    FullName = x.Name
                })
                .ToArray();
            var found = symbols
                .Where(x => x.Symbol.Contains(querySafe, StringComparison.InvariantCultureIgnoreCase) ||
                            x.Ticker.Contains(querySafe, StringComparison.InvariantCultureIgnoreCase) ||
                            x.Description.Contains(querySafe, StringComparison.InvariantCultureIgnoreCase))
                .Take(limit ?? 100)
                .ToArray();
            return Task.FromResult(found);
        }

        public async Task<TvBarResponse> GetHistory(DateTime @from, DateTime to, string symbol, string resolution)
        {
            await Task.Yield();

            var key = $"{symbol}";
            if (Bars.ContainsKey(key))
            {
                var bars = Bars[key];
                return FindBars(from, to, bars);
            }

            return new TvBarResponse()
            {
                Bars = new TvBar[0],
                Status = TvBarStatus.NoData
            };
        }

        public async Task<TvMark[]> GetMarks(DateTime @from, DateTime to, string symbol, string resolution)
        {
            await Task.Yield();

            var key = $"{symbol}";
            if (Marks.ContainsKey(key))
            {
                var marks = Marks[key];
                return FindMarks(from, to, marks);
            }

            return new TvMark[0];
        }

        private TvBarResponse FindBars(DateTime @from, DateTime to, TvBar[] bars)
        {
            var foundBars = bars
                .Where(x => x.Timestamp >= RemoveTime(from) && x.Timestamp <= RemoveTime(to))
                .OrderBy(x => x.Timestamp)
                .ToArray();
            var before = bars
                .OrderBy(x => x.Timestamp)
                .LastOrDefault(x => x.Timestamp < RemoveTime(@from));
            return new TvBarResponse()
            {
                Bars = foundBars,
                Status = foundBars.Any() ? TvBarStatus.Ok : TvBarStatus.NoData,
                NextTime = before?.Timestamp
            };
        }

        private TvMark[] FindMarks(DateTime @from, DateTime to, TvMark[] marks)
        {
            var foundMarks = marks
                .Where(x => x.Timestamp >= RemoveTime(from) && x.Timestamp <= RemoveTime(to))
                .OrderBy(x => x.Timestamp)
                .ToArray();
            return foundMarks;
        }

        private DateTime RemoveTime(in DateTime timestamp)
        {
            return timestamp;
            //return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 
            //    0, 0, 0, DateTimeKind.Utc);
        }


       
    }
}