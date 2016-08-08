using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.DataModel.Enums;

namespace piSensorNet.Logic
{
    public interface IMainHubEngine
    {
        void Error(string clientID, string message);
        void NewModuleFunctions(int moduleID, IReadOnlyCollection<FunctionTypeEnum> functions);
        void NewModule(int moduleID, string moduleAddress);
        void NewTemperatureReading(int moduleID, int sensorID, decimal value, DateTime created, DateTime received);
        void NewTemperatureSensor(int moduleID, int sensorID, string sensorAddress);
        void ChangedTemperatureSensorPeriod(int moduleID, TimeSpan period);
        void NewOneWireDevices(int moduleID, IReadOnlyDictionary<int, string> devices);
        void NewVoltageReading(int moduleID, decimal value, DateTime created, DateTime received);
    }
}