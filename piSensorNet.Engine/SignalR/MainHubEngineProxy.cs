using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common.Enums;
using piSensorNet.Logic;

namespace piSensorNet.Engine.SignalR
{
    internal sealed class MainHubEngineProxy : IMainHubEngine
    {
        private readonly IHubProxy _proxy;

        public MainHubEngineProxy(IHubProxy proxy)
        {
            _proxy = proxy;
        }

        public void Error(string clientID, string message)
        {
            _proxy.Invoke(nameof(Error), clientID, message);
        }

        public void NewModuleFunctions(int moduleID, IReadOnlyCollection<KeyValuePair<FunctionTypeEnum, string>> functions)
        {
            _proxy.Invoke(nameof(NewModuleFunctions), moduleID, functions);
        }

        public void NewModule(int moduleID, string moduleAddress)
        {
            _proxy.Invoke(nameof(NewModule), moduleID, moduleAddress);
        }

        public void NewTemperatureReading(int moduleID, int sensorID, decimal value, DateTime created, DateTime received)
        {
            _proxy.Invoke(nameof(NewTemperatureReading), moduleID, sensorID, value, created, received);
        }

        public void NewTemperatureSensor(int moduleID, int sensorID, string sensorAddress)
        {
            _proxy.Invoke(nameof(NewTemperatureSensor), moduleID, sensorID, sensorAddress);
        }

        public void ChangedTemperatureSensorPeriod(int moduleID, TimeSpan period)
        {
            _proxy.Invoke(nameof(ChangedTemperatureSensorPeriod), moduleID, period);
        }

        public void NewOneWireDevices(int moduleID, IReadOnlyDictionary<int, string> devices)
        {
            _proxy.Invoke(nameof(NewOneWireDevices), moduleID, devices);
        }

        public void NewVoltageReading(int moduleID, decimal value, DateTime created, DateTime received)
        {
            _proxy.Invoke(nameof(NewVoltageReading), moduleID, value, created, received);
        }
    }
}
