using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal static class Interrupts
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiISR")]
        public static extern int InterruptServiceRoutine(int pin, int interruptType, InterruptCallback handler);

        [DllImport(Constants.LibraryPath, EntryPoint = "waitForInterrupt")]
        public static extern int WaitForInterrupt(int pin, int timeout);
    }
}