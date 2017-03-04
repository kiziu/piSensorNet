using System;
using System.Collections.Generic;
using System.Linq;

namespace piSensorNet.Logic.Triggers
{
    public delegate void TriggerDelegate(TriggerDelegateContext context);

    public sealed class TriggerDelegateContext
    {
        public TriggerDelegateContext(DateTime now, IReadOnlyDictionary<string, object> properties)
        {
            Now = now;
            Properties = properties;
        }

        public DateTime Now { get; }
        
        public IReadOnlyDictionary<string, object> Properties { get; }
    }
}