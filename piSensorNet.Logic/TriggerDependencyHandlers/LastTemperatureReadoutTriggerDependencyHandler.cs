using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.Logic.TriggerDependencyHandlers.Base;
using piSensorNet.Logic.Triggers;

namespace piSensorNet.Logic.TriggerDependencyHandlers
{
    internal sealed class LastTemperatureReadoutTriggerDependencyHandler : ITriggerDependencyHandler
    {
        public TriggerDependencyTypeEnum TriggerDependencyType => TriggerDependencyTypeEnum.LastTemperatureReadout;
        public bool IsModuleIdentityRequired => true;

        public void Handle(PiSensorNetDbContext databaseContext, TriggerDelegateContext context, int? moduleID)
        {
            // TODO KZ: check filtration performance
            var readouts = databaseContext.TemperatureReadouts
                                          .Where(i => i.TemperatureSensor.ModuleID == moduleID.Value)
                                          .GroupBy(i => i.TemperatureSensorID)
                                          .Select(i => new
                                                       {
                                                           TemperatureSensorID = i.Key,
                                                           Max = i.Select(ii => ii.Received).Max()
                                                       });

            var sensors = databaseContext.TemperatureSensors.Where(i => i.ModuleID == moduleID.Value);

            var readings = databaseContext.TemperatureReadouts
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

            context.LastTemperatureReadoutsByAddress = byAddress;
            context.LastTemperatureReadoutsByFriendlyName = byFriendlyName;
        }
    }
}