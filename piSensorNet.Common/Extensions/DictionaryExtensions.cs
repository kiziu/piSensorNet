using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}