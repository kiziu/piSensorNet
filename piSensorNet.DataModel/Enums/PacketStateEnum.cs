﻿using System;
using System.Linq;

namespace piSensorNet.DataModel.Enums
{
    public enum PacketStateEnum
    {
        Unknown = 0,
        New = 1,
        Handled = 2,
        Unhandled = 4,
        Redundant = 5,
        Skipped = 6,
    }
}