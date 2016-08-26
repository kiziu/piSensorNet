using System;
using System.Globalization;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.Logic;

using static piSensorNet.Common.Helpers.LoggingHelper;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub : IMainHubClient
    {
        public void Identify(int? moduleID)
        {
            ToConsole($"{nameof(Identify)}({moduleID?.ToString() ?? "<null>"}) ${Context.ConnectionId}");

            Engine?.SendMessage(Context.ConnectionId, moduleID, FunctionTypeEnum.Identify);
        }

        public void ReadTemperature(int? moduleID)
        {
            ToConsole($"{nameof(ReadTemperature)}({moduleID?.ToString() ?? "<null>"}) ${Context.ConnectionId}");

            Engine?.SendMessage(Context.ConnectionId, moduleID, FunctionTypeEnum.OwDS18B20Temperature);
        }

        public void SetTemperatureReportPeriod(int moduleID, TimeSpan period)
        {
            ToConsole($"{nameof(SetTemperatureReportPeriod)}({moduleID}, {period}) ${Context.ConnectionId}");

            var value = (int)(period.TotalMilliseconds / 100);

            if (value < 0 || value > UInt16.MaxValue)
            {
                SendError($"Period '{period}' chosen for module #{moduleID} is out of valid range.");

                return;
            }

            var text = value.ToString("D", CultureInfo.InvariantCulture);

            Engine?.SendMessage(Context.ConnectionId, moduleID, FunctionTypeEnum.OwDS18B20TemperaturePeriodical, text);
        }
    }
}