using System;
using System.Linq;

namespace piSensorNet.Common.System
{
    public static class Constants
    {
        public static readonly bool IsWindows = Environment.OSVersion.VersionString.IndexOf("Windows", StringComparison.InvariantCultureIgnoreCase) >= 0;
    }
}
