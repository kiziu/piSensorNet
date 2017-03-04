using System;
using System.Linq;
using piSensorNet.Common.Custom.Interfaces;
using piSensorNet.WiringPi.Enums;

namespace piSensorNet.Radio.Configuration
{
    public interface IRadioConfiguration : IReadOnlyConfiguration
    {
        PinNumberEnum InterruptPin { get; }
        PinNumberEnum ChipEnablePin { get; }
        SpiChannelEnum SpiChannel { get; }

        string HubAddress { get; }
        string BroadcastAddress { get; }
    }
}