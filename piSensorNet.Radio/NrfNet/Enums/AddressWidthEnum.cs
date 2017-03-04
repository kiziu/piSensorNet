using System;
using System.Linq;

namespace piSensorNet.Radio.NrfNet.Enums
{
    public enum AddressWidthEnum : byte
    {
        Illegal = 0,
        ThreeBytes,
        FourBytes,
        FiveBytes
    }
}