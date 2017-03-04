using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal static class Sleep
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "delay")]
        public static extern int Milli(uint milliseconds);

        [DllImport(Constants.LibraryPath, EntryPoint = "delayMicroseconds")]
        public static extern int Micro(uint microseconds);
    }
}