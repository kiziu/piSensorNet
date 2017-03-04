using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal static class Spi
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiSPISetup")]
        public static extern int Setup(int channel, int speed);

        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiSPIDataRW")]
        public static extern int Exchange(int channel, byte[] data, int dataLength);
    }
}