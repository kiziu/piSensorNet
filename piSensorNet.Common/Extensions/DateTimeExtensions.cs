using System;
using System.Linq;

namespace piSensorNet.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime TruncateMilliseconds(this DateTime dateTime, int precision)
        {
            if (0 <= precision && precision > 7) throw new ArgumentOutOfRangeException(nameof(precision));

            var precisionPower = (long)Math.Pow(10, 7 - precision);
            var difference = dateTime.Ticks % precisionPower;

            return new DateTime(dateTime.Ticks - difference, dateTime.Kind);
        }

        public static DateTime ReplaceTime(this DateTime dateTime, TimeSpan time)
        {
            if (time.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(time), time, "Time cannot be ngative.");

            if (time.Days >= 1)
                throw new ArgumentOutOfRangeException(nameof(time), time, "Time cannot be greater than or equal to 24h.");

            var ticks = dateTime.Ticks;
            ticks -= ticks % TimeSpan.TicksPerDay;
            ticks += time.Ticks;

            return new DateTime(ticks);
        }

        public static String ToFullTimeString(this DateTime dateTime)
            => dateTime.ToString("HH:mm:ss.ffffff");
    }
}