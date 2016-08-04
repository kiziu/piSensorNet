using System;
using System.Linq;

namespace piSensorNet.DataModel.Enums
{
    public enum MessageStateEnum
    {
        Unknown = 0,
        Queued = 1,
        Sent = 2,
        Completed = 3,
        Failed = 4
    }
}