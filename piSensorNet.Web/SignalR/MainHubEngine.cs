using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.Logic;

using static piSensorNet.Common.Helpers.LoggingHelper;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub : IMainHubEngine
    {
        public void Error(string clientID, string message)
        {
            ToConsole($"{nameof(Error)}({clientID}, {message})");

            if (!IsEngine())
                return;

            SendError(clientID, message);
        }

        public void NewModuleFunctions(int moduleID, IReadOnlyCollection<KeyValuePair<FunctionTypeEnum, string>> functions)
        {
            ToConsole($"{nameof(NewModuleFunctions)}({moduleID}, [{functions.Select(i => i.ToString()).Join(", ")}])");
            if (!IsEngine())
                return;

            NonEngine.OnNewModuleFunctions(moduleID, functions);
        }

        public void NewModule(int moduleID, string moduleAddress)
        {
            ToConsole($"{nameof(NewModule)}({moduleID}, {moduleAddress})");
            if (!IsEngine())
                return;

            NonEngine.OnNewModule(moduleID, moduleAddress);
        }

        public void NewTemperatureReading(int moduleID, int sensorID, decimal value, DateTime created, DateTime received)
        {
            ToConsole($"{nameof(NewTemperatureReading)}({moduleID}, {sensorID}, {value}, {created}, {received})");
            if (!IsEngine())
                return;

            NonEngine.OnNewTemperatureReading(moduleID, sensorID, value, created, received);
        }

        public void NewTemperatureSensor(int moduleID, int sensorID, string sensorAddress)
        {
            ToConsole($"{nameof(NewTemperatureSensor)}({moduleID}, {sensorID}, {sensorAddress})");
            if (!IsEngine())
                return;

            NonEngine.OnNewTemperatureSensor(moduleID, sensorID, sensorAddress);
        }

        public void ChangedTemperatureSensorPeriod(int moduleID, TimeSpan period)
        {
            ToConsole($"{nameof(ChangedTemperatureSensorPeriod)}({moduleID}, {period})");
            if (!IsEngine())
                return;

            NonEngine.OnChangedTemperatureSensorPeriod(moduleID, period);
        }

        public void NewOneWireDevices(int moduleID, IReadOnlyDictionary<int, string> devices)
        {
            ToConsole($"{nameof(NewOneWireDevices)}({moduleID}, {{{devices.Select(i => $"{i.Key}: {i.Value}").Join(", ")}}})");
            if (!IsEngine())
                return;

            NonEngine.OnNewOneWireDevices(moduleID, devices);
        }

        public void NewVoltageReading(int moduleID, decimal value, DateTime created, DateTime received)
        {
            ToConsole($"{nameof(NewVoltageReading)}({moduleID}, {value}, {created}, {received})");
            if (!IsEngine())
                return;

            NonEngine.OnNewVoltageReading(moduleID, value, created, received);
        }
    }
}