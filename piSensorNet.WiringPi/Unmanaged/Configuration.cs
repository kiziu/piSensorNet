using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal static class Configuration
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiSetup")]
        public static extern int Setup();
    }
}