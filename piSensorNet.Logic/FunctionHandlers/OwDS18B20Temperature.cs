using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using piSensorNet.Common;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class OwDS18B20Temperature : TemperatureSensorsFinderBase<KeyValuePair<string, string>>
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.OwDS18B20Temperature;
        public override TriggerSourceTypeEnum? TriggerSourceType => TriggerSourceTypeEnum.TemperatureReadout;

        protected override IReadOnlyCollection<KeyValuePair<string, string>> GetItems(IpiSensorNetConfiguration moduleConfiguration, Packet packet)
            => FunctionHandlerHelper.SplitPairs(packet.Text, moduleConfiguration.FunctionResultDelimiter, moduleConfiguration.FunctionResultValueDelimiter);

        protected override Func<KeyValuePair<string, string>, string> GetAddress => pair => pair.Key;

        protected override void ItemCallback(FunctionHandlerContext context, Packet packet, HubMessageQueue hubMessageQueue, KeyValuePair<string, string> item, TemperatureSensor sensor, bool wasSensorCreated)
        {
            decimal reading;
            if(!decimal.TryParse(item.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out reading))
            {
                Log(context, packet,
                    MethodBase.GetCurrentMethod().GetFullName(),
                    $"Could not parse text '{item.Value}' to decimal temperature.");

                return;
            }
            
            var temperatureReading = new TemperatureReadout(sensor.ID, reading, packet.Received);

            context.DatabaseContext.TemperatureReadouts.Add(temperatureReading);

            hubMessageQueue.Enqueue(i => i.NewTemperatureReading(packet.Module.ID, temperatureReading.TemperatureSensorID, temperatureReading.Value, temperatureReading.Created, temperatureReading.Received));
        }
    }
}