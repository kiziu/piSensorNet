using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    // http://wiringpi.com/reference/serial-library/
    internal static class Serial
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "serialOpen", CharSet = CharSet.Ansi)]
        public static extern int Open([MarshalAs(UnmanagedType.LPStr)] string device, int baud);

        [DllImport(Constants.LibraryPath, EntryPoint = "serialClose")]
        public static extern void Close(int fd);

        [DllImport(Constants.LibraryPath, EntryPoint = "serialPutchar")]
        public static extern void Put(int fd, byte c);

        [DllImport(Constants.LibraryPath, EntryPoint = "serialPuts", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Put(int fd, string s);

        [DllImport(Constants.LibraryPath, EntryPoint = "serialDataAvail")]
        public static extern int DataAvailable(int fd);

        [DllImport(Constants.LibraryPath, EntryPoint = "serialGetchar", CharSet = CharSet.Ansi)]
        public static extern int Get(int fd);

        [DllImport(Constants.LibraryPath, EntryPoint = "serialFlush")]
        public static extern void Flush(int fd);
    }
}
