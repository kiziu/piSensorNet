using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal static class Interrupts
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiISR", SetLastError = true)]
        public static extern int InterruptServiceRoutine(int pin, int interruptType, InterruptCallback handler);

        [DllImport(Constants.LibraryPath, EntryPoint = "waitForInterrupt", SetLastError = true)]
        public static extern int WaitForInterrupt(int pin, int timeout);
    }
}