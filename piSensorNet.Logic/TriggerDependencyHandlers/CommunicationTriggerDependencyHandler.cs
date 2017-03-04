using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Context;
using piSensorNet.Logic.TriggerDependencyHandlers.Base;

namespace piSensorNet.Logic.TriggerDependencyHandlers
{
    internal sealed class CommunicationTriggerDependencyHandler : BaseTriggerDependencyHandler<CommunicationTriggerDependencyHandler>
    {
        public override TriggerDependencyTypeEnum TriggerDependencyType { get; } = TriggerDependencyTypeEnum.Communication;
        public override bool IsModuleIdentityRequired { get; } = false;

        public override IReadOnlyDictionary<string, object> Handle(PiSensorNetDbContext context, int? moduleID)
        {
            throw new NotImplementedException();
        }
    }
}