using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal static class Spi
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiSPISetup", SetLastError = true)]
        public static extern int Setup(int channel, int speed);

        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiSPIDataRW", SetLastError = true)]
        public static extern int Exchange(int channel, [In, Out] byte[] data, int dataLength);
    }
}