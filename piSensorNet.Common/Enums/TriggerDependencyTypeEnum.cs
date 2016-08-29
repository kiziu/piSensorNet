using System;
using System.Linq;

namespace piSensorNet.Common.Enums
{
    public enum TriggerDependencyTypeEnum
    {
        Unknown = 0,
        Communication = 1,
        LastTemperatureReadout = 2,
    }
}