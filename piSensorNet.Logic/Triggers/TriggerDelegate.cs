using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Custom;

namespace piSensorNet.Logic.Triggers
{
    public delegate void TriggerDelegate(TriggerDelegateContext context);

    public sealed class TriggerDelegateContext
    {
        public TriggerDelegateContext(DateTime now, IReadOnlyDictionary<string, TypedObject> properties)
        {
            Now = now;
            Properties = properties;
        }

        public DateTime Now { get; }
        
        public IReadOnlyDictionary<string, TypedObject> Properties { get; }
    }
}