using System;
using System.Linq;

namespace piSensorNet.Common
{
    public interface IModuleConfiguration
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

    public class ModuleConfiguration : IModuleConfiguration
    {
        public ModuleConfiguration(IConfiguration configuration)
        {
            FunctionResultValueDelimiter = configuration["Settings:FunctionResultValueDelimiter"].Single();
            FunctionResultDelimiter = configuration["Settings:FunctionResultDelimiter"].Single();
            FunctionResultNameDelimiter = configuration["Settings:FunctionResultNameDelimiter"].Single();
            MessageIDDelimiter = configuration["Settings:MessageIDDelimiter"].Single();
            FunctionQuery = configuration["Settings:FunctionQuery"].Single();
            FunctionDelimiter = configuration["Settings:FunctionDelimiter"].Single();
            AddressDelimiter = configuration["Settings:AddressDelimiter"].Single();
            PeriodUnitLengthInMs = int.Parse(configuration["Settings:PeriodUnitLengthInMs"]);
            BroadcastAddress = configuration["Settings:BroadcastAddress"];
            AddressPattern = configuration["Settings:AddressPattern"];
            ConnectionString = configuration["Settings:ConnectionString"];
        }

        public string ConnectionString { get; }
        public string AddressPattern { get; }
        public string BroadcastAddress { get; }
        public int PeriodUnitLengthInMs { get; }

        /// <summary>
        /// @
        /// </summary>
        public char AddressDelimiter { get; }

        /// <summary>
        /// :
        /// </summary>
        public char FunctionDelimiter { get; }

        /// <summary>
        /// ?
        /// </summary>
        public char FunctionQuery { get; }

        /// <summary>
        /// #
        /// </summary>
        public char MessageIDDelimiter { get; }

        /// <summary>
        /// =
        /// </summary>
        public char FunctionResultNameDelimiter { get; }

        /// <summary>
        /// ;
        /// </summary>
        public char FunctionResultDelimiter { get; }

        /// <summary>
        /// |
        /// </summary>
        public char FunctionResultValueDelimiter { get; }
    }
}