using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

[assembly: InternalsVisibleTo("piSensorNet.Tests")]

namespace piSensorNet.Common
{
    public interface IConfiguration
    {
        string this[string key] { get; }
    }

    public static class Configuration
    {
        public static IConfiguration Load(params string[] configFiles)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("globalConfig.json", true) // windows
                .AddJsonFile(@"..\..\..\..\..\globalConfig.json", true) // windows
                .AddJsonFile("../globalConfig.json", true); // linux

            foreach (var configFile in configFiles)
                builder.AddJsonFile(configFile);

            var configuration = builder.Build();

            return new Proxy(configuration);
        }

        private class Proxy : IConfiguration
        {
            private readonly IConfigurationRoot _configuration;
            public Proxy(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }

            public string this[string key] => _configuration[key];
        }
    }
}
