using System;
using System.Collections.Generic;
using piSensorNet.Common.Enums;

namespace piSensorNet.Web.SignalR.Interfaces
{
    public interface IClient
    {
        void OnNewModuleFunctions(int moduleID, IReadOnlyCollection<KeyValuePair<FunctionTypeEnum, string>> functions);
        void OnError(string message);
        void OnNewModule(int moduleID, string moduleAddress);
        void OnNewTemperatureReading(int moduleID, int sensorID, decimal value, DateTime created, DateTime received);
        void OnNewTemperatureSensor(int moduleID, int sensorID, string sensorAddress);
        void OnChangedTemperatureSensorPeriod(int moduleID, TimeSpan period);
        void OnNewOneWireDevices(int moduleID, IReadOnlyDictionary<int, string> devices);
        void OnNewVoltageReading(int moduleID, decimal value, DateTime created, DateTime received);
    }
}