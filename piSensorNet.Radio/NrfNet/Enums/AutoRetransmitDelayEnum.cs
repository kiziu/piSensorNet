using System;
using System.Linq;

namespace piSensorNet.Radio.NrfNet.Enums
{
    public enum AutoRetransmitDelayEnum : byte
    {
        T250us = 0,
        T500us,
        T750us,
        T1000us,
        T1250us,
        T1500us,
        T1750us,
        T2000us,
        T2250us,
        T2500us,
        T2750us,
        T3000us,
        T3250us,
        T3500us,
        T3750us,
        T4000us
    }
}