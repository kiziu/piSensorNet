using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void InterruptCallback();

    internal static class Constants
    {
        public const string LibraryPath = "libwiringPi.so";
    }
}