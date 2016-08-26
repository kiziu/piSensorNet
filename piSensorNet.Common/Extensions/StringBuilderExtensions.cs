using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class StringBuilderExtensions
    {
        [NotNull]
        public static StringBuilder RegexReplace([NotNull] this StringBuilder builder, [RegexPattern] [NotNull] string pattern, [NotNull] string replacement, RegexOptions options = RegexOptions.None)
        {
            var matches = Regex.Matches(builder.ToString(), pattern, options);
            var offset = 0;
            foreach (Match match in matches)
            {
                var expandedReplacement = match.Result(replacement);

                builder.Remove(match.Index + offset, match.Length);
                builder.Insert(match.Index + offset, expandedReplacement);

                offset += expandedReplacement.Length - match.Length;
            }

            return builder;
        }

        [NotNull]
        public static StringBuilder Replace([NotNull] this StringBuilder builder, [NotNull] IEnumerable<KeyValuePair<string, string>> pairs, Func<string, string> modificator = null)
        {
            foreach (var pair in pairs)
                builder.Replace(pair.Key, modificator != null ? modificator(pair.Value) : pair.Value);

            return builder;
        }
    }
}