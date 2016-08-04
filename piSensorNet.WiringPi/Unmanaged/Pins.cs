using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal static class Pins
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "pinMode", SetLastError = true)]
        public static extern void Mode(int pin, int mode);

        [DllImport(Constants.LibraryPath, EntryPoint = "pullUpDnControl", SetLastError = true)]
        public static extern void PullUpSetup(int pin, int type);

        [DllImport(Constants.LibraryPath, EntryPoint = "digitalWrite", SetLastError = true)]
        public static extern void DigitalWrite(int pin, int value);

        [DllImport(Constants.LibraryPath, EntryPoint = "digitalRead", SetLastError = true)]
        public static extern int DigitalRead(int pin);

        [DllImport(Constants.LibraryPath, EntryPoint = "pwmWrite", SetLastError = true)]
        public static extern void PwmWrite(int pin, int value);

        [DllImport(Constants.LibraryPath, EntryPoint = "analogRead", SetLastError = true)]
        public static extern int AnalogRead(int pin);
    }
}