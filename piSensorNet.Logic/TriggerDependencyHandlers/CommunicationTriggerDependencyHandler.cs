using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Enums;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Context;
using piSensorNet.Logic.TriggerDependencyHandlers.Base;

namespace piSensorNet.Logic.TriggerDependencyHandlers
{
    internal sealed class CommunicationTriggerDependencyHandler : ITriggerDependencyHandler
    {
        private static readonly IReadOnlyDictionary<string, decimal> LastTemperatureReadoutsByAddress = null;
        private static readonly IReadOnlyDictionary<string, decimal> LastTemperatureReadoutsByFriendlyName = null;

        private static readonly Dictionary<string, Type> properties
            = new Dictionary<string, Type>
              {
                  {nameof(LastTemperatureReadoutsByAddress), Reflector.Static.Field(() => LastTemperatureReadoutsByAddress).FieldType},
                  {nameof(LastTemperatureReadoutsByFriendlyName), Reflector.Static.Field(() => LastTemperatureReadoutsByFriendlyName).FieldType},
              };

        public IReadOnlyDictionary<string, Type> Properties { get; } = properties;
        public TriggerDependencyTypeEnum TriggerDependencyType { get; } = TriggerDependencyTypeEnum.Communication;
        public bool IsModuleIdentityRequired { get; } = false;

        public IReadOnlyDictionary<string, TypedObject> Handle(PiSensorNetDbContext context, int? moduleID)
        {
            throw new NotImplementedException();
        }
    }
}