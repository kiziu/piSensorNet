using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Context;
using piSensorNet.Logic.TriggerDependencyHandlers.Base;
using piSensorNet.Logic.Triggers;

namespace piSensorNet.Logic.TriggerSourceHandlers.Base
{
    public class TriggerSourceHandlerHelperContext : TriggerSourceHandlerContext
    {
        public TriggerSourceHandlerHelperContext(PiSensorNetDbContext databaseContext, IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> triggerSourceHandlers, IReadOnlyDictionary<int, TriggerDelegate> triggerDelegates, IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> triggerDependencyHandlers, DateTime triggerDateTime)
            : base(databaseContext)
        {
            TriggerSourceHandlers = triggerSourceHandlers;
            TriggerDelegates = triggerDelegates;
            TriggerDependencyHandlers = triggerDependencyHandlers;

            TriggerDateTime = triggerDateTime;
        }

        public IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> TriggerSourceHandlers { get; set; }
        public IReadOnlyDictionary<int, TriggerDelegate> TriggerDelegates { get; set; }
        public IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> TriggerDependencyHandlers { get; set; }

        public DateTime TriggerDateTime { get; set; }
    }
}