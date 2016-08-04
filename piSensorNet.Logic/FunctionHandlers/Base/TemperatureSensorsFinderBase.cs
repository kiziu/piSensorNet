using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.Logic.Custom;

namespace piSensorNet.Logic.FunctionHandlers.Base
{
    internal abstract class TemperatureSensorsFinderBase<T> : IFunctionHandler
    {
        public abstract FunctionTypeEnum FunctionType { get; }

        protected abstract IReadOnlyCollection<T> GetItems(IModuleConfiguration moduleConfiguration, Packet packet);
        protected abstract Func<T, string> GetAddress { get; }

        protected virtual void ItemCallback(PiSensorNetDbContext context, Packet packet, Queue<Func<IHubProxy, Task>> hubtasksQueue, T item, TemperatureSensor sensor, bool wasSensorCreated) { }
        protected virtual void OnHandled(PiSensorNetDbContext context, Module module, Queue<Func<IHubProxy, Task>> hubtasksQueue, IReadOnlyCollection<TemperatureSensor> newSensors) { }

        public FunctionHandlerResult Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, ref Queue<Func<IHubProxy, Task>> hubTasksQueue)
        {
            if (packet.Module.State != ModuleStateEnum.Identified)
                return PacketStateEnum.Skipped;

            var items = GetItems(moduleConfiguration, packet);
            var module = packet.Module;
            var moduleTemperatureSensors = context.TemperatureSensors
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

                    context.TemperatureSensors.Add(temperatureSensor);
                    moduleTemperatureSensors.Add(temperatureSensor.Address, temperatureSensor);
                    newSensors.Add(temperatureSensor);

                    isAdded = true;
                    context.SaveChanges();
                }

                ItemCallback(context, packet, hubTasksQueue, item, temperatureSensor, isAdded);
            }
            
            if (newSensors.Count > 0)
            {
                var message = new Message(functions[FunctionTypeEnum.OwDS18B20TemperaturePeriodical].Key, true)
                              {
                                  ModuleID = module.ID,
                              };

                context.Messages.Add(message);
            }

            context.SaveChanges();

            OnHandled(context, module, hubTasksQueue, newSensors);

            return new FunctionHandlerResult(PacketStateEnum.Handled, newSensors.Count > 0);
        }
    }
}