using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Custom.Interfaces;

namespace piSensorNet.Common.Custom
{
    public class Map<TForwardKey, TReverseKey> : IMap<TForwardKey, TReverseKey>
    {
        public Map(Dictionary<TForwardKey, TReverseKey> forward, Dictionary<TReverseKey, TForwardKey> reverse)
        {
            Forward = forward;
            Reverse = reverse;
        }

        public Map(Dictionary<TForwardKey, TReverseKey> forward)
        {
            Forward = forward;
            Reverse = forward.ToDictionary(i => i.Value, i => i.Key);
        }

        public Map(int capacity)
        {
            Forward = new Dictionary<TForwardKey, TReverseKey>(capacity);
            Reverse = new Dictionary<TReverseKey, TForwardKey>(capacity);
        }

        public Map()
            : this(0) {}

        public Dictionary<TForwardKey, TReverseKey> Forward { get; }
        public Dictionary<TReverseKey, TForwardKey> Reverse { get; }

        IDictionary<TForwardKey, TReverseKey> IMap<TForwardKey, TReverseKey>.Forward => Forward;
        IDictionary<TReverseKey, TForwardKey> IMap<TForwardKey, TReverseKey>.Reverse => Reverse;

        IReadOnlyDictionary<TForwardKey, TReverseKey> IReadOnlyMap<TForwardKey, TReverseKey>.Forward => Forward;
        IReadOnlyDictionary<TReverseKey, TForwardKey> IReadOnlyMap<TForwardKey, TReverseKey>.Reverse => Reverse;
    }

    public static class Map
    {
        public static Map<TForwardKey, TReverseKey> Create<TItem, TForwardKey, TReverseKey>(IEnumerable<TItem> source, Func<TItem, TForwardKey> forwardKeySelector, Func<TItem, TReverseKey> reverseKeySelector, int capacity = 0)
        {
            var forward = new Dictionary<TForwardKey, TReverseKey>(capacity);
            var reverse = new Dictionary<TReverseKey, TForwardKey>(capacity);

            foreach (var item in source)
            {
                var forwardKey = forwardKeySelector(item);
                var reverseKey = reverseKeySelector(item);

                forward.Add(forwardKey, reverseKey);
                reverse.Add(reverseKey, forwardKey);
            }

            return new Map<TForwardKey, TReverseKey>(forward, reverse);
        }
    }
}
