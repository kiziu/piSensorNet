using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.DataModel.Enums;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub
    {
        public void Error(string clientID, string message)
        {
            Console.WriteLine($"{Now}: Error");

            if (!IsEngine())
                return;

            SendError(clientID, message);
        }

        public void NewModuleFunctions(int moduleID, IReadOnlyDictionary<FunctionTypeEnum, string> functions)
        {
            Console.WriteLine($"{Now}: NewModuleFunctions");
            if (!IsEngine())
                return;

            NonEngine.onNewModuleFunctions(moduleID, functions);
        }

        public void NewModule(int moduleID, string moduleAddress)
        {
            Console.WriteLine($"{Now}: NewModule");
            if (!IsEngine())
                return;

            NonEngine.onNewModule(moduleID, moduleAddress);
        }

        public void NewTemperatureReading(int moduleID, int sensorID, decimal value, DateTime created, DateTime received)
        {
            Console.WriteLine($"{Now}: NewTemperatureReading");
            if (!IsEngine())
                return;

            NonEngine.onNewTemperatureReading(moduleID, sensorID, value, created, received);
        }

        public void ChangedTemperatureSensorPeriod(int moduleID, TimeSpan period)
        {
            Console.WriteLine($"{Now}: ChangedTemperatureSensorPeriod");
            if (!IsEngine())
                return;

            NonEngine.onChangedTemperatureSensorPeriod(moduleID, period);
        }

        public void NewOneWireDevices(int moduleID, IReadOnlyDictionary<int, string> devices)
        {
            Console.WriteLine($"{Now}: NewOneWireDevices");
            if (!IsEngine())
                return;

            NonEngine.onNewOneWireDevices(moduleID, devices);
        }

        public void NewVoltageReading(int moduleID, decimal value, DateTime created, DateTime received)
        {
            Console.WriteLine($"{Now}: NewVoltageReading");
            if (!IsEngine())
                return;

            NonEngine.onNewVoltageReading(moduleID, value, created, received);
        }
    }
}