using System;
using System.Linq;
using piSensorNet.Common.Custom.Interfaces;
using piSensorNet.WiringPi.Enums;

namespace piSensorNet.Common
{
    public interface IpiSensorNetConfiguration : IReadOnlyConfiguration
    {
        string ConnectionString { get; }

        string AddressPattern { get; }
        string BroadcastAddress { get; }
        string HubAddress { get; }

        PinNumberEnum InterruptPin { get; }
        PinNumberEnum ChipEnablePin { get; }
        SpiChannelEnum SpiChannel { get; }

        int PeriodUnitLengthInMs { get; }

        /// <summary>
        /// @
        /// </summary>
        char AddressDelimiter { get; }

        /// <summary>
        /// :
        /// </summary>
        char FunctionDelimiter { get; }

        /// <summary>
        /// ?
        /// </summary>
        char FunctionQuery { get; }

        /// <summary>
        /// #
        /// </summary>
        char MessageIDDelimiter { get; }

        /// <summary>
        /// =
        /// </summary>
        char FunctionResultNameDelimiter { get; }

        /// <summary>
        /// ;
        /// </summary>
        char FunctionResultDelimiter { get; }

        /// <summary>
        /// |
        /// </summary>
        char FunctionResultValueDelimiter { get; }
    }
}