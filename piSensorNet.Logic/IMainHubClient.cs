using System;
using System.Linq;

namespace piSensorNet.Logic
{
    public interface IMainHubClient
    {
        void Identify(int? moduleID);
        void ReadTemperature(int? moduleID);
        void SetTemperatureReportPeriod(int moduleID, TimeSpan period);
    }
}