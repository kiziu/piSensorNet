using System;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.TriggerSourceHandlers.Base;

namespace piSensorNet.Logic.TriggerSourceHandlers
{
    internal sealed class AbsoluteTimeTriggerSourceHandler : ITriggerSourceHandler
    {
        public TriggerSourceTypeEnum TriggerSourceType => TriggerSourceTypeEnum.AbsoluteTime;

        public void Handle(TriggerSourceHandlerContext triggerSourceHandlerContext, TriggerSource source)
        {
            // ReSharper disable once PossibleInvalidOperationException
            source.NextAbsoluteTimeExecution = DateTime.Now.AddDays(1).ReplaceTime(source.AbsoluteTime.Value);

            triggerSourceHandlerContext.DatabaseContext.EnqueueUpdate<TriggerSource>(
                i => i.ID == source.ID,
                i => i.NextAbsoluteTimeExecution == source.NextAbsoluteTimeExecution);
                
            triggerSourceHandlerContext.DatabaseContext.SaveChanges();
        }
    }
}