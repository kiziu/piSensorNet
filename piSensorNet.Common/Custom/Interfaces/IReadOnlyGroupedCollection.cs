using System;
using System.Collections.Generic;
using System.Linq;

namespace piSensorNet.Common.Custom.Interfaces
{
    public interface IReadOnlyGroupedCollection<TKey, out TElement> : IReadOnlyCollection<TElement>
    {
        TKey Key { get; }
    }
}
