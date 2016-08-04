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
    }
}