using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Custom;

namespace piSensorNet.Logic.FunctionHandlers.Base
{
    internal abstract class TemperatureSensorsFinderBase<T> : FunctionHandlerBase
    {
        protected abstract IReadOnlyCollection<T> GetItems(IpiSensorNetConfiguration moduleConfiguration, Packet packet);
        protected abstract Func<T, string> GetAddress { get; }

        protected virtual void ItemCallback(FunctionHandlerContext context, Packet packet, HubMessageQueue hubMessageQueue, T item, TemperatureSensor sensor, bool wasSensorCreated) { }
        protected virtual void OnHandled(FunctionHandlerContext context, Module module, HubMessageQueue hubMessageQueue, IReadOnlyCollection<TemperatureSensor> newSensors) { }

        public sealed override FunctionHandlerResult Handle(FunctionHandlerContext context, Packet packet, ref HubMessageQueue hubMessageQueue)
        {
            var items = GetItems(context.ModuleConfiguration, packet);
            var module = packet.Module;
            var moduleTemperatureSensors = context.DatabaseContext.TemperatureSensors
                                                  .AsNoTracking()
                                                  .Where(i => i.ModuleID == module.ID)
                                                  .ToDictionary(i => i.Address, i => i);

            var newSensors = new List<TemperatureSensor>();
            foreach (var item in items)
            {
                var address = GetAddress(item).ToUpper();
                var isAdded = false;
                TemperatureSensor temperatureSensor;

                if (moduleTemperatureSensors.ContainsKey(address))
                    temperatureSensor = moduleTemperatureSensors[address];
                else
                {
                    temperatureSensor = new TemperatureSensor(module, address);

                    context.DatabaseContext.TemperatureSensors.Add(temperatureSensor);
                    moduleTemperatureSensors.Add(temperatureSensor.Address, temperatureSensor);
                    newSensors.Add(temperatureSensor);

                    isAdded = true;
                    context.DatabaseContext.SaveChanges();

                    hubMessageQueue.Enqueue(i => i.NewTemperatureSensor(temperatureSensor.ModuleID, temperatureSensor.ID, temperatureSensor.Address));
                }

                ItemCallback(context, packet, hubMessageQueue, item, temperatureSensor, isAdded);
            }
            
            if (newSensors.Count > 0)
            {
                var message = new Message(context.FunctionTypes.Forward[FunctionTypeEnum.OwDS18B20TemperaturePeriodical], true)
                              {
                                  ModuleID = module.ID,
                              };

                context.DatabaseContext.Messages.Add(message);
            }

            context.DatabaseContext.SaveChanges();

            OnHandled(context, module, hubMessageQueue, newSensors);

            return new FunctionHandlerResult(PacketStateEnum.Handled, newSensors.Count > 0);
        }
    }
}