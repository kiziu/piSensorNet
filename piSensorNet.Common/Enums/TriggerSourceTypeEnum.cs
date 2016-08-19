using System;
using System.Linq;

namespace piSensorNet.Common.Enums
{
    public enum TriggerSourceTypeEnum
    {
        Unknown = 0,
        AbsoluteTime = 1,
        TemperatureReadout = 2,
        VoltageReadout = 3
    }
}