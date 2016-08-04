using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class StringExtensions
    {
        public static string SubstringBetween(this string haystack, string leftNeedle,
            string rightNeedle, bool includeNeedles = false)
        {
            var leftNeeedlePosition = haystack.IndexOf(leftNeedle, StringComparison.InvariantCulture);
            var rightNeeedlePosition = haystack.IndexOf(rightNeedle, StringComparison.InvariantCulture);
            if (leftNeeedlePosition < 0 || rightNeeedlePosition < 0 || leftNeeedlePosition >= rightNeeedlePosition)
                return null;

            if (includeNeedles)
                rightNeeedlePosition += rightNeedle.Length;
            else
                leftNeeedlePosition += leftNeedle.Length;

            var substring = haystack.Substring(leftNeeedlePosition, rightNeeedlePosition - leftNeeedlePosition);

            return substring;
        }

        public static string SubstringAfter(this string haystack, string needle,
            bool includeNeedle = false)
        {
            var neeedlePosition = haystack.IndexOf(needle, StringComparison.InvariantCulture);
            if (neeedlePosition < 0)
                return null;

            if (!includeNeedle)
                neeedlePosition += needle.Length;

            var substring = haystack.Substring(neeedlePosition);

            return substring;
        }

        public static string SubstringBefore(this string haystack, string needle,
            bool includeNeedle = false)
        {
            var neeedlePosition = haystack.IndexOf(needle, StringComparison.InvariantCulture);
            if (neeedlePosition < 0)
                return null;

            if (includeNeedle)
                neeedlePosition += needle.Length;

            var substring = haystack.Substring(0, neeedlePosition);

            return substring;
        }

        public static string SubstringAfter(this string haystack, char needle,
            bool includeNeedle = false)
        {
            var neeedlePosition = haystack.IndexOf(needle);
            if (neeedlePosition < 0)
                return null;

            if (!includeNeedle)
                neeedlePosition += 1;

            var substring = haystack.Substring(neeedlePosition);

            return substring;
        }

        public static string SubstringBefore(this string haystack, char needle,
            bool includeNeedle = false)
        {
            var neeedlePosition = haystack.IndexOf(needle);
            if (neeedlePosition < 0)
                return null;

            if (includeNeedle)
                neeedlePosition += 1;

            var substring = haystack.Substring(0, neeedlePosition);

            return substring;
        }

        public static bool Contains(this string haystack, string needle, StringComparison comparisonType)
            => haystack.IndexOf(needle, comparisonType) >= 0;

        public static string Join(this IEnumerable<string> input, string separator)
            => String.Join(separator, input);

        public static string Concat(this IEnumerable<string> input)
            => String.Concat(input);

        [StringFormatMethod("pattern")]
        public static string AsFormatFor(this string pattern, params object[] args) 
            => String.Format(pattern, args);

        public static IEnumerable<string> Chunkify(this string input, int length)
        {
            if (input.Length == 0)
                yield break;

            if (input.Length <= length)
            {
                yield return input;
                yield break;
            }

            for (var index = 0; index < input.Length; index += length)
            {
                var chunkLength = length;
                if (index + length > input.Length)
                    chunkLength = input.Length - index;

                yield return input.Substring(index, chunkLength);
            }
        }
    }
}
