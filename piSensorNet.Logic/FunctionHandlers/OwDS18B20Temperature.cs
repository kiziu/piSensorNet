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
}