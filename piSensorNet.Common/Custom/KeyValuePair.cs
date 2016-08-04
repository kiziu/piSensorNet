using System;
using System.Collections.Generic;
using System.Linq;

namespace piSensorNet.Common.Custom
{
    public static class KeyValuePair
    {
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
            => new KeyValuePair<TKey, TValue>(key, value);
    }
}
