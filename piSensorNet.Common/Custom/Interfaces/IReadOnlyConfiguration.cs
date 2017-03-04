using System;
using System.Linq;

namespace piSensorNet.Common.Custom.Interfaces
{
    public interface IReadOnlyConfiguration
    {
        string this[string key] { get; }
    }
}