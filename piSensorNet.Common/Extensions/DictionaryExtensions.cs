using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class DictionaryExtensions
    {
        [CanBeNull]
        public static TValue GetValueOrDefault<TKey, TValue>([NotNull] this IReadOnlyDictionary<TKey, TValue> dictionary, [NotNull] TKey key)
        {
            TValue value;

            dictionary.TryGetValue(key, out value);

            return value;
        }

        [CanBeNull]
        public static TValue? GetValueOrNullable<TKey, TValue>([NotNull] this IReadOnlyDictionary<TKey, TValue> dictionary, [NotNull] TKey key)
            where TValue : struct
        {
            TValue value;

            if (!dictionary.TryGetValue(key, out value))
                return null;

            return value;
        }

        [CanBeNull]
        public static TValue GetValueOrAdd<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, [NotNull] TKey key, [InstantHandle][NotNull] Func<TKey, TValue> valueCreator)
        {
            TValue value;

            if (dictionary.TryGetValue(key, out value))
                return value;

            value = valueCreator(key);

            dictionary.Add(key, value);

            return value;
        }

        /// <summary>
        /// Does not alter the underlying dictionary.
        /// </summary>
        [NotNull]
        public static IReadOnlyDictionary<TKey, TValue> ReadOnly<TKey, TValue>([NotNull] this IReadOnlyDictionary<TKey, TValue> dictionary)
            => dictionary;

        [NotNull]
        public static IDictionary<TKey, TValue> AddOrReplace<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, [NotNull] TKey key, [CanBeNull] TValue value)
        {
            dictionary[key] = value;

            return dictionary;
        }

        [NotNull]
        public static Dictionary<TKey, TValue> Add<TKey, TValue>([NotNull] this Dictionary<TKey, TValue> dictionary, [NotNull] IEnumerable<KeyValuePair<TKey, TValue>> toAdd)
        {
            var iDictionary = (IDictionary<TKey, TValue>)dictionary;

            foreach (var entry in toAdd)
                iDictionary.Add(entry);

            return dictionary;
        }

        [NotNull]
        public static IDictionary<TKey, TValue> With<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, [NotNull] TKey key, [CanBeNull] TValue value)
        {
            dictionary.Add(key, value);

            return dictionary;
        }
    }
}