using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.Logic.TriggerDependencyHandlers.Base;

namespace piSensorNet.Logic.TriggerDependencyHandlers
{
    internal sealed class LastTemperatureReadoutTriggerDependencyHandler : BaseTriggerDependencyHandler<LastTemperatureReadoutTriggerDependencyHandler>
    {
        [UsedImplicitly]
        private IReadOnlyDictionary<string, decimal> LastTemperatureReadoutsByAddress;
        [UsedImplicitly]
        private IReadOnlyDictionary<string, decimal> LastTemperatureReadoutsByFriendlyName;

        //private static readonly Dictionary<string, Type> properties
        //    = new Dictionary<string, Type>
        //      {
        //          {nameof(LastTemperatureReadoutsByAddress), Reflector.Static.Field(() => LastTemperatureReadoutsByAddress).FieldType},
        //          {nameof(LastTemperatureReadoutsByFriendlyName), Reflector.Static.Field(() => LastTemperatureReadoutsByFriendlyName).FieldType},
        //      };

        //public IReadOnlyDictionary<string, Type> Properties { get; } = properties;
        public override TriggerDependencyTypeEnum TriggerDependencyType => TriggerDependencyTypeEnum.LastTemperatureReadout;
        public override bool IsModuleIdentityRequired => true;

        public override IReadOnlyDictionary<string, TypedObject> Handle(PiSensorNetDbContext context, int? moduleID)
        {
            // TODO KZ: check filtration performance
            var readouts = context.TemperatureReadouts
                                  .Where(i => i.TemperatureSensor.ModuleID == moduleID.Value)
                                  .GroupBy(i => i.TemperatureSensorID)
                                  .Select(i => new
                                               {
                                                   TemperatureSensorID = i.Key,
                                                   Max = i.Select(ii => ii.Received).Max()
                                               });

            var sensors = context.TemperatureSensors.Where(i => i.ModuleID == moduleID.Value);

            var readings = context.TemperatureReadouts
                                  .Join(readouts,
                                      i => i.TemperatureSensorID,
                                      i => i.TemperatureSensorID,
                                      (l, r) => new {l, r})
                                  .Where(i => i.l.Received == i.r.Max)
                                  .Join(sensors,
                                      i => i.l.TemperatureSensorID,
                                      i => i.ID,
                                      (l, r) => new {r.ID, r.Address, r.FriendlyName, l.l.Value, l.l.Received})
                                  .ToList();

            var byAddress = new Dictionary<string, decimal>(readings.Count);
            var byFriendlyName = new Dictionary<string, decimal>(readings.Count);

            foreach (var reading in readings)
            {
                byAddress.Add(reading.Address, reading.Value);

                if (reading.FriendlyName != null)
                    byFriendlyName.AddOrReplace(reading.FriendlyName, reading.Value);
            }

            LastTemperatureReadoutsByAddress = byAddress;
            LastTemperatureReadoutsByFriendlyName = byFriendlyName;

            return ToProperties();

            //return new Dictionary<string, TypedObject>(properties.Count)
            //       {
            //           {
            //               nameof(LastTemperatureReadoutsByAddress),
            //               byAddress.ToTyped(properties[nameof(LastTemperatureReadoutsByAddress)])
            //           },
            //           {
            //               nameof(LastTemperatureReadoutsByFriendlyName),
            //               byFriendlyName.ToTyped(properties[nameof(LastTemperatureReadoutsByFriendlyName)])
            //           },
            //       };
        }
    }
}