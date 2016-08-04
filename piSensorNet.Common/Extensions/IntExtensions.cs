using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace piSensorNet.Common.Extensions
{
    public static class IntExtensions
    {
        private static readonly char[] Base36Characters =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a',
            'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l',
            'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w',
            'x', 'y', 'z'
        };

        private static readonly IReadOnlyDictionary<char, int> InverseBase36Characters =
            Base36Characters.Select((c, i) => new
                                              {
                                                  c,
                                                  i
                                              }).ToDictionary(i => i.c, i => i.i);

        public static string ToBase36(this int intValue, int length = 1)
        {
            var builder = new StringBuilder(length);

            do
            {
                builder.Insert(0, Base36Characters[intValue % 36]);
                intValue = intValue / 36;
            } while (intValue > 0);

            return builder.ToString();
        }

        public static int FromBase36(this string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
                throw new ArgumentNullException(nameof(stringValue));

            var value = 0;

            foreach (var c in stringValue)
            {
                value *= 36;
                value += InverseBase36Characters[c];
            }

            return value;
        }
    }
}