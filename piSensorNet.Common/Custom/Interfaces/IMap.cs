using System;
using System.Collections.Generic;
using System.Linq;

namespace piSensorNet.Common.Custom.Interfaces
{
    public interface IMap<TForwardKey, TReverseKey> : IReadOnlyMap<TForwardKey, TReverseKey>
    {
        new IDictionary<TForwardKey, TReverseKey> Forward { get; }
        new IDictionary<TReverseKey, TForwardKey> Reverse { get; }
    }
}