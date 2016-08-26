using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;

            dictionary.TryGetValue(key, out value);

            return value;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;

            dictionary.TryGetValue(key, out value);

            return value;
        }

        public static TValue? GetValueOrNullable<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue: struct 
        {
            TValue value;

            if (!dictionary.TryGetValue(key, out value))
                return null;

            return value;
        }

        public static TValue? GetValueOrNullable<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
            where TValue: struct 
        {
            TValue value;

            if (!dictionary.TryGetValue(key, out value))
                return null;

            return value;
        }

        /// <summary>
        /// Does not alter the underlying dictionary.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> ReadOnly<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary) 
            => dictionary;

        [NotNull]
        public static IDictionary<TKey, TValue> AddOrReplace<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary[key] = value;

            return dictionary;
        }

        [NotNull]
        public static Dictionary<TKey, TValue> Add<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> toAdd)
        {
            var iDictionary = (IDictionary<TKey, TValue>)dictionary;

            foreach (var entry in toAdd)
                iDictionary.Add(entry);

            return dictionary;
        }

        [NotNull]
        public static IDictionary<TKey, TValue> With<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary.Add(key, value);

            return dictionary;
        }
    }
}