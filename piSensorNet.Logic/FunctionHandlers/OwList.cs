using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    // TODO KZ: extend functionality once more OW device types present

    /// <summary>
    ///  for now, assume OW devices are tempreature sensors
    /// </summary>
    internal sealed class OwList : TemperatureSensorsFinderBase<string>
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.OwList;

        protected override IReadOnlyCollection<string> GetItems(IModuleConfiguration moduleConfiguration, Packet packet)
            => FunctionHandlerHelper.SplitSingle(packet.Text, moduleConfiguration.FunctionResultDelimiter);

        protected override Func<string, string> GetAddress => item => item;

        protected override void OnHandled(PiSensorNetDbContext context, Module module, Queue<Func<IHubProxy, Task>> hubtasksQueue, IReadOnlyCollection<TemperatureSensor> newSensors)
        {
            var dictionary = newSensors.ToDictionary(i => i.ID, i => i.Address);

            hubtasksQueue.Enqueue(proxy => proxy.Invoke("newOneWireDevices", module.ID, dictionary));
        }
    }

    /*
    /// <summary>
    ///  for now, assume OW devices are tempreature sensors
    /// </summary>
    internal sealed class OwList : IFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.OwList;
        public void Handle(PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, int> functions)
        {
            var devices = FunctionHandlerHelper.SplitSingle(packet.Text);
            var module = packet.Module;
            var moduleTemperatureSensors = context.TemperatureSensors
                                                  .Where(i => i.ModuleID == module.ID)
                                                  .ToDictionary(i => i.Address, i => i);
            var newSensorsAdded = false;

            foreach (var device in devices)
            {
                var address = device.ToUpper();

                if (moduleTemperatureSensors.ContainsKey(address))
                    continue;

                var temperatureSensor = new TemperatureSensor
                                        {
                                            Address = address,
                                        };

                module.TemperatureSensors.Add(temperatureSensor);
                moduleTemperatureSensors.Add(temperatureSensor.Address, temperatureSensor);

                newSensorsAdded = true;
            }

            if (newSensorsAdded)
            {
                var message = new Message
                {
                    ModuleID = module.ID,
                    FunctionID = functions[FunctionTypeEnum.OwDS18B20TemperaturePeriodical],
                    IsQuery = true,
                };

                context.Messages.Add(message);
            }

            context.SaveChanges();
        }
    }
    */
}
