using System;
using System.Globalization;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.Logic;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub : IMainHubClient
    {
        public void Identify(int? moduleID)
        {
            Console.WriteLine($"{Now}: {nameof(Identify)}({moduleID?.ToString() ?? "<null>"}) ${Context.ConnectionId}");

            Engine.SendMessage(Context.ConnectionId, moduleID, FunctionTypeEnum.Identify);
        }

        public void ReadTemperature(int? moduleID)
        {
            Console.WriteLine($"{Now}: {nameof(ReadTemperature)}({moduleID?.ToString() ?? "<null>"}) ${Context.ConnectionId}");

            Engine.SendMessage(Context.ConnectionId, moduleID, FunctionTypeEnum.OwDS18B20Temperature);
        }

        public void SetTemperatureReportPeriod(int moduleID, TimeSpan period)
        {
            Console.WriteLine($"{Now}: {nameof(SetTemperatureReportPeriod)}({moduleID}, {period}) ${Context.ConnectionId}");

            var value = (int)(period.TotalMilliseconds / 100);

            if (value < 0 || value > UInt16.MaxValue)
            {
                SendError($"Period '{period}' chosen for module #{moduleID} is out of valid range.");

                return;
            }

            var text = value.ToString("D", CultureInfo.InvariantCulture);

            Engine.SendMessage(Context.ConnectionId, moduleID, FunctionTypeEnum.OwDS18B20TemperaturePeriodical, text);
        }
    }
}