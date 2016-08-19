using System;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities;

namespace piSensorNet.Logic.TriggerSourceHandlers.Base
{
    public interface ITriggerSourceHandler
    {
        TriggerSourceTypeEnum TriggerSourceType { get; }

        void Handle(TriggerSourceHandlerContext triggerSourceHandlerContext, TriggerSource source);
    }
}
