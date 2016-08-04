using System;
using System.Linq;

namespace piSensorNet.Common
{
    public interface IModuleConfiguration : IConfiguration
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
        private readonly IConfiguration _configuration;

        public ModuleConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;

            FunctionResultValueDelimiter = _configuration["Settings:FunctionResultValueDelimiter"].Single();
            FunctionResultDelimiter = _configuration["Settings:FunctionResultDelimiter"].Single();
            FunctionResultNameDelimiter = _configuration["Settings:FunctionResultNameDelimiter"].Single();
            MessageIDDelimiter = _configuration["Settings:MessageIDDelimiter"].Single();
            FunctionQuery = _configuration["Settings:FunctionQuery"].Single();
            FunctionDelimiter = _configuration["Settings:FunctionDelimiter"].Single();
            AddressDelimiter = _configuration["Settings:AddressDelimiter"].Single();
            PeriodUnitLengthInMs = int.Parse(_configuration["Settings:PeriodUnitLengthInMs"]);
            BroadcastAddress = _configuration["Settings:BroadcastAddress"];
            AddressPattern = _configuration["Settings:AddressPattern"];
            ConnectionString = _configuration["Settings:ConnectionString"];
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

        public string this[string key] => _configuration[key];
    }
}