using System;
using System.Collections.Generic;
using System.Linq;

namespace piSensorNet.Logic.Triggers
{
    public delegate void TriggerDelegate(TriggerDelegateContext context);

    public sealed class TriggerDelegateContext
    {
        public IReadOnlyDictionary<string, decimal> LastTemperatureReadoutsByAddress { get; internal set; }
        public IReadOnlyDictionary<string, decimal> LastTemperatureReadoutsByFriendlyName{ get; internal set; }
    }
}
