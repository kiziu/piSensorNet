using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Enums;
using piSensorNet.Logic;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub : IMainHubEngine
    {
        public void Error(string clientID, string message)
        {
            Console.WriteLine($"{Now}: {nameof(Error)}({clientID}, {message})");

            if (!IsEngine())
                return;

            SendError(clientID, message);
        }

        public void NewModuleFunctions(int moduleID, IReadOnlyCollection<FunctionTypeEnum> functions)
        {
            Console.WriteLine($"{Now}: {nameof(NewModuleFunctions)}({moduleID}, [{functions.Select(i => i.ToString()).Join(", ")}]");
            if (!IsEngine())
                return;

            NonEngine.onNewModuleFunctions(moduleID, functions);
        }

        public void NewModule(int moduleID, string moduleAddress)
        {
            Console.WriteLine($"{Now}: {nameof(NewModule)}({moduleID}, {moduleAddress})");
            if (!IsEngine())
                return;

            NonEngine.onNewModule(moduleID, moduleAddress);
        }

        public void NewTemperatureReading(int moduleID, int sensorID, decimal value, DateTime created, DateTime received)
        {
            Console.WriteLine($"{Now}: {nameof(NewTemperatureReading)}({moduleID}, {sensorID}, {value}, {created}, {received})");
            if (!IsEngine())
                return;

            NonEngine.onNewTemperatureReading(moduleID, sensorID, value, created, received);
        }

        public void NewTemperatureSensor(int moduleID, int sensorID, string sensorAddress)
        {
            Console.WriteLine($"{Now}: {nameof(NewTemperatureSensor)}({moduleID}, {sensorID}, {sensorAddress})");
            if (!IsEngine())
                return;

            NonEngine.onNewTemperatureSensor(moduleID, sensorID, sensorAddress);
        }

        public void ChangedTemperatureSensorPeriod(int moduleID, TimeSpan period)
        {
            Console.WriteLine($"{Now}: {nameof(ChangedTemperatureSensorPeriod)}({moduleID}, {period})");
            if (!IsEngine())
                return;

            NonEngine.onChangedTemperatureSensorPeriod(moduleID, period);
        }

        public void NewOneWireDevices(int moduleID, IReadOnlyDictionary<int, string> devices)
        {
            Console.WriteLine($"{Now}: {nameof(NewOneWireDevices)}({moduleID}, {{{devices.Select(i => $"{i.Key}: {i.Value}").Join(", ")}}})");
            if (!IsEngine())
                return;

            NonEngine.onNewOneWireDevices(moduleID, devices);
        }

        public void NewVoltageReading(int moduleID, decimal value, DateTime created, DateTime received)
        {
            Console.WriteLine($"{Now}: {nameof(NewVoltageReading)}({moduleID}, {value}, {created}, {received})");
            if (!IsEngine())
                return;

            NonEngine.onNewVoltageReading(moduleID, value, created, received);
        }
    }
}