using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class StringExtensions
    {
        [CanBeNull]
        [ContractAnnotation("null => null")]
        public static string TrimToNull([CanBeNull] this string input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return null;

            return input.Trim();
        }

        public static string SubstringBetween(this string haystack, string leftNeedle,
            string rightNeedle, bool includeNeedles = false)
        {
            var leftNeeedlePosition = haystack.IndexOf(leftNeedle, StringComparison.InvariantCulture);
            var rightNeeedlePosition = leftNeeedlePosition < haystack.Length ? haystack.IndexOf(rightNeedle, leftNeeedlePosition + 1, StringComparison.InvariantCulture) : -1;
            if (leftNeeedlePosition < 0 || rightNeeedlePosition < 0)
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

        public static string RemoveEnd(this string haystack, string needle, StringComparison comparisonType = StringComparison.InvariantCulture)
        {
            if (!haystack.EndsWith(needle, comparisonType))
                return haystack;

            return haystack.Substring(0, haystack.Length - needle.Length);
        }
    }
}
