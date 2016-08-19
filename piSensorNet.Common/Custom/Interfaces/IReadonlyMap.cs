using System;
using System.Collections.Generic;
using System.Linq;

namespace piSensorNet.Common.Custom.Interfaces
{
    public interface IReadOnlyMap<TForwardKey, TReverseKey>
    {
        IReadOnlyDictionary<TForwardKey, TReverseKey> Forward { get; }
        IReadOnlyDictionary<TReverseKey, TForwardKey> Reverse { get; }
    }
}