using System;
using System.Linq;

namespace piSensorNet.Common.Configuration
{
    public interface IReadOnlyConfiguration
    {
        string this[string key] { get; }
    }
}