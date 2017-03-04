using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Triggers;

namespace piSensorNet.Logic.TriggerSourceHandlers.Base
{
    public static class TriggerSourceHandlerHelper
    {
        public static void Handle(TriggerSourceHandlerHelperContext context, TriggerSource triggerSource, int? moduleID = null)
        {
            var trigger = triggerSource.Trigger;
            
            var triggerSourceHandler = context.TriggerSourceHandlers[triggerSource.Type];
            var method = context.TriggerDelegates[triggerSource.TriggerID];

            var properties = new Dictionary<string, object>();
            foreach (var triggerDependency in trigger.TriggerDependencies)
            {
                var triggerDependencyHandler = context.TriggerDependencyHandlers.GetValueOrDefault(triggerDependency.Type);
                if (triggerDependencyHandler == null)
                    continue;

                if (triggerDependencyHandler.IsModuleIdentityRequired && !moduleID.HasValue)
                    continue;

                properties.Add(triggerDependencyHandler.Handle(context.DatabaseContext, moduleID));
            }
            
            var methodContext = new TriggerDelegateContext(context.TriggerDateTime, properties);

            method(methodContext);

            triggerSourceHandler?.Handle(context, triggerSource);
        }
    }
}
