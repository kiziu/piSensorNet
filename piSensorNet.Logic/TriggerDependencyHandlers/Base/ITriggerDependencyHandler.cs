using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Context;

namespace piSensorNet.Logic.TriggerDependencyHandlers.Base
{
    public interface ITriggerDependencyHandler
    {
        IReadOnlyDictionary<string, Type> Properties { get; }
        TriggerDependencyTypeEnum TriggerDependencyType { get; }
        bool IsModuleIdentityRequired { get; }

        IReadOnlyDictionary<string, object> Handle(PiSensorNetDbContext context, int? moduleID);
    }
}