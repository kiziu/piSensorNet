using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Custom.Interfaces;

namespace piSensorNet.Common.Custom
{
    public class GroupedList<TKey, TElement> : List<TElement>, IReadOnlyGroupedCollection<TKey, TElement>
    {
        public TKey Key { get; }

        public GroupedList([NotNull] IEnumerable<TElement> collection, TKey key)
            : base(collection)
        {
            Key = key;
        }

        public GroupedList(TKey key, int capacity)
            : base(capacity)
        {
            Key = key;
        }

        public GroupedList(TKey key)
            : this(key, 0) {}
    }
}
