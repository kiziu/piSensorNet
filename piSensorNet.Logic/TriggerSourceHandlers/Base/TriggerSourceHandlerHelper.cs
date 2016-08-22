using System;
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
            
            var methodContext = new TriggerDelegateContext(context.TriggerDateTime);

            foreach (var triggerDependency in trigger.TriggerDependencies)
            {
                var triggerDependencyHandler = context.TriggerDependencyHandlers.GetValueOrDefault(triggerDependency.Type);
                if (triggerDependencyHandler == null)
                    continue;

                if (triggerDependencyHandler.IsModuleIdentityRequired && !moduleID.HasValue)
                    continue;

                triggerDependencyHandler.Handle(context.DatabaseContext, methodContext, moduleID);
            }
            
            method(methodContext);

            triggerSourceHandler?.Handle(context, triggerSource);
        }
    }
}
