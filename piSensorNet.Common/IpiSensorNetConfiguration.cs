using System;
using System.Linq;
using piSensorNet.Common.Custom.Interfaces;

namespace piSensorNet.Common
{
    public interface IpiSensorNetConfiguration : IReadOnlyConfiguration
    {
        string ConnectionString { get; }

        string AddressPattern { get; }

        string BroadcastAddress { get; }

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