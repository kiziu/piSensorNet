using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace piSensorNet.Common.Custom.Interfaces
{
    public interface IReadOnlyGroupedCollection<TKey, out TElement> : IReadOnlyCollection<TElement>
    {
        TKey Key { get; }
    }
}
