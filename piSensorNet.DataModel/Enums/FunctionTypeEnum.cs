using System;
using System.Linq;

namespace piSensorNet.DataModel.Enums
{
    public enum FunctionTypeEnum
    {
        Unknown= 0,
        Identify = 1,
        FunctionList = 2,
        Voltage = 3,
        Report = 4,
        OwList = 5,
        OwDS18B20Temperature = 6,
        OwDS18B20TemperaturePeriodical = 7,
    }
}