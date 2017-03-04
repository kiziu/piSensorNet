using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal static class Pins
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "pinMode")]
        public static extern void Mode(int pin, int mode);

        [DllImport(Constants.LibraryPath, EntryPoint = "pullUpDnControl")]
        public static extern void PullUpSetup(int pin, int type);

        [DllImport(Constants.LibraryPath, EntryPoint = "digitalWrite")]
        public static extern void DigitalWrite(int pin, int value);

        [DllImport(Constants.LibraryPath, EntryPoint = "digitalRead")]
        public static extern int DigitalRead(int pin);

        [DllImport(Constants.LibraryPath, EntryPoint = "pwmWrite")]
        public static extern void PwmWrite(int pin, int value);

        [DllImport(Constants.LibraryPath, EntryPoint = "analogRead")]
        public static extern int AnalogRead(int pin);
    }
}