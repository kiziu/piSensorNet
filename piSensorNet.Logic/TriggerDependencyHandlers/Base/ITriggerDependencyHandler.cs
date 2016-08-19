using System;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Context;
using piSensorNet.Logic.Triggers;

namespace piSensorNet.Logic.TriggerDependencyHandlers.Base
{
    public interface ITriggerDependencyHandler
    {
        TriggerDependencyTypeEnum TriggerDependencyType { get; }
        bool IsModuleIdentityRequired { get; }

        void Handle(PiSensorNetDbContext databaseContext, TriggerDelegateContext context, int? moduleID);
    }
}
