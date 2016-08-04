using System;
using System.Collections.Generic;
using System.Globalization;
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
    internal sealed class OwDS18B20Temperature : TemperatureSensorsFinderBase<KeyValuePair<string, string>>
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.OwDS18B20Temperature;

        protected override IReadOnlyCollection<KeyValuePair<string, string>> GetItems(IModuleConfiguration moduleConfiguration, Packet packet)
            => FunctionHandlerHelper.SplitPairs(packet.Text, moduleConfiguration.FunctionResultDelimiter, moduleConfiguration.FunctionResultValueDelimiter);

        protected override Func<KeyValuePair<string, string>, string> GetAddress => pair => pair.Key;

        protected override void ItemCallback(PiSensorNetDbContext context, Packet packet, Queue<Func<IHubProxy, Task>> hubtasksQueue, KeyValuePair<string, string> item, TemperatureSensor sensor, bool wasSensorCreated)
        {
            var reading = decimal.Parse(item.Value, CultureInfo.InvariantCulture);

            var temperatureReading = new TemperatureReading(sensor.ID, reading, packet.Received);

            context.TemperatureReadings.Add(temperatureReading);

            hubtasksQueue.Enqueue(proxy =>
                proxy.Invoke("newTemperatureReading", packet.Module.ID, sensor.ID, temperatureReading.Value, temperatureReading.Created, temperatureReading.Received));
        }
    }

    /*
    internal sealed class OwDS18B20Temperature : IFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.OwDS18B20Temperature;

        public void Handle(PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, int> functions)
        {
            var sensorReadings = FunctionHandlerHelper.SplitPairs(packet.Text, EngineMain.FunctionResultValueDelimiter);
            var module = packet.Module;
            var moduleTemperatureSensors = context.TemperatureSensors
                                                  .Where(i => i.ModuleID == module.ID)
                                                  .ToDictionary(i => i.Address, i => i);
            var newSensorsAdded = false;

            foreach (var sensorReading in sensorReadings)
            {
                var address = sensorReading.Key.ToUpper();
                var reading = decimal.Parse(sensorReading.Value, CultureInfo.InvariantCulture);
                TemperatureSensor temperatureSensor;

                if (moduleTemperatureSensors.ContainsKey(address))
                    temperatureSensor = moduleTemperatureSensors[address];
                else
                {
                    temperatureSensor = new TemperatureSensor
                    {
                        Address = address,
                    };

                    module.TemperatureSensors.Add(temperatureSensor);
                    moduleTemperatureSensors.Add(temperatureSensor.Address, temperatureSensor);

                    newSensorsAdded = true;
                }

                var temperatureReading = new TemperatureReading
                {
                    Value = reading,
                    Received = packet.Received,
                };

                temperatureSensor.TemperatureReadings.Add(temperatureReading);
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