using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using piSensorNet.DataModel.Enums;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub
    {
        public void Identify()
        {
            Console.WriteLine($"{Now}: Identify from {Context.ConnectionId}");

            Engine.SendMessage(null, FunctionTypeEnum.Identify);
        }

        public void ReadTemperature(int? moduleID)
        {
            Console.WriteLine($"{Now}: ReadTemperature({moduleID?.ToString() ?? "<null>"}) from {Context.ConnectionId}");

            Engine.SendMessage(moduleID, FunctionTypeEnum.OwDS18B20Temperature);
        }

        public void SetTemperatureReportPeriod(int moduleID, TimeSpan period)
        {
            Console.WriteLine($"{Now}: SetTemperatureReportPeriod({moduleID}, {period}) from {Context.ConnectionId}");

            var value = (int)(period.TotalMilliseconds / 100);

            if (value < 0 || value > UInt16.MaxValue)
            {
                SendError($"Period '{period}' chosen for module #{moduleID} is out of valid range.");

                return;
            }

            var text = value.ToString("D", CultureInfo.InvariantCulture);

            Engine.SendMessage(moduleID, FunctionTypeEnum.OwDS18B20TemperaturePeriodical, text);
        }

        public async Task<IReadOnlyDictionary<int, string>> ListModules()
        {
            Console.WriteLine($"{Now}: ListModules from {Context.ConnectionId}");

            return await Task.Factory.StartNew(_modulesService.List);
        }
    }
}