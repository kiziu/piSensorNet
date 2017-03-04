using System;
using System.Linq;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public interface IRegister
    {
        RegisterEnum Type { get; }
        byte Value { get; }
    }
}