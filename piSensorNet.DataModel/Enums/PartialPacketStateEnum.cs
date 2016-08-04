using System;
using System.Linq;

namespace piSensorNet.DataModel.Enums
{
    public enum PartialPacketStateEnum
    {
        Unknown = 0,
        New = 1,
        Fragmented = 2,
        Merged = 3,
    }
}