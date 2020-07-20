using System;

namespace SimpleBacktester
{
    public static class DateUtils
    {
        public static readonly DateTime UnixBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ConvertToTimeFromSec(double timeInSec)
        {
            var unixTimeStampInTicks = (long)(timeInSec * TimeSpan.TicksPerSecond);
            return new DateTime(UnixBase.Ticks + unixTimeStampInTicks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Convert DateTime into unix seconds with high resolution (6 decimal places for milliseconds)
        /// </summary>
        public static double ToUnixSeconds(this DateTime date)
        {
            var unixTimeStampInTicks = (date.ToUniversalTime() - UnixBase).Ticks;
            return (double)unixTimeStampInTicks / TimeSpan.TicksPerSecond;
        }

        public static double? ToUnixSeconds(in this DateTime? date)
        {
            if (!date.HasValue)
                return null;
            return ToUnixSeconds(date.Value);
        }
    }
}
