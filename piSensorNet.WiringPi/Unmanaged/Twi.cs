using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace piSensorNet.WiringPi.Unmanaged
{
    internal class Twi
    {
        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiI2CSetup")]
        public static extern int Setup(int deviceId);

        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiI2CRead")]
        public static extern int Read(int deviceId);

        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiI2CWrite")]
        public static extern int Write(int deviceId, int data);

        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiI2CWriteReg8")]
        public static extern int WriteReg8(int deviceId, int register, int data);

        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiI2CWriteReg16")]
        public static extern int WriteReg16(int deviceId, int register, int data);

        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiI2CReadReg8")]
        public static extern int ReadReg8(int deviceId, int register);

        [DllImport(Constants.LibraryPath, EntryPoint = "wiringPiI2CReadReg16")]
        public static extern int ReadReg16(int deviceId, int register);
    }
}