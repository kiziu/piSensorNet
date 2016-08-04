using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using piSensorNet.Common.System;
using piSensorNet.WiringPi.Managed.Enums;
using piSensorNet.WiringPi.Unmanaged;

namespace piSensorNet.WiringPi.Managed
{
    public static class Pi
    {
        static Pi()
        {
            var result = Configuration.Setup();

            if (result == 1)
                throw new Exception("Could not setup wiringPi. Must be root.");

            if (result < 0 || result > 0)
                throw new Exception("Could not setup wiringPi.");
        }

        private static void Init()
        {
            RuntimeHelpers.RunClassConstructor(typeof(Pi).TypeHandle);
        }

        public static class Pins
        {
            static Pins()
            {
                Init();
            }

            public static void Setup(WiringPiPinNumberEnum pin, PinModeEnum mode, PullUpModeEnum? pullUpMode = null)
            {
                InternalSetup((int)pin, mode, pullUpMode);
            }

            public static void Setup(BroadcomPinNumberEnum pin, PinModeEnum mode, PullUpModeEnum? pullUpMode = null)
            {
                InternalSetup((int)pin, mode, pullUpMode);
            }

            private static void InternalSetup(int pin, PinModeEnum mode, PullUpModeEnum? pullUpMode)
            {
                if (mode == PinModeEnum.Input && !pullUpMode.HasValue)
                    throw new Exception("Input must have pull-up configured.");

                if (mode != PinModeEnum.Input && pullUpMode.HasValue)
                    throw new Exception("Only input can have pull-up configured.");

                Unmanaged.Pins.Mode(pin, (int)mode);

                if (pullUpMode.HasValue)
                    Unmanaged.Pins.PullUpSetup(pin, (int)pullUpMode.Value);
            }

            public static void Write(WiringPiPinNumberEnum pin, bool state)
            {
                InternalWrite((int)pin, state);
            }

            public static void Write(BroadcomPinNumberEnum pin, bool state)
            {
                InternalWrite((int)pin, state);
            }

            private static void InternalWrite(int pin, bool state)
            {
                var intState = Convert.ToInt32(state);

                Unmanaged.Pins.DigitalWrite(pin, intState);
            }

            public static bool Read(WiringPiPinNumberEnum pin)
            {
                return InternalRead((int)pin);
            }

            public static bool Read(BroadcomPinNumberEnum pin)
            {
                return InternalRead((int)pin);
            }

            private static bool InternalRead(int pin)
            {
                var intState = Unmanaged.Pins.DigitalRead(pin);
                var state = intState > 0;

                return state;
            }
        }

        public static class Interrupts
        {
            static Interrupts()
            {
                Init();
            }

            public static void Setup(WiringPiPinNumberEnum pin, InterruptModeEnum mode, Action handler)
            {
                InternalSetup((int)pin, mode, handler);
            }

            public static void Setup(BroadcomPinNumberEnum pin, InterruptModeEnum mode, Action handler)
            {
                InternalSetup((int)pin, mode, handler);
            }

            public static IDisposable SetupPolled(WiringPiPinNumberEnum pin, InterruptModeEnum mode, Action handler)
            {
                return InternalSetupPolled((int)pin, mode, handler);
            }

            public static IDisposable SetupPolled(BroadcomPinNumberEnum pin, InterruptModeEnum mode, Action handler)
            {
                return InternalSetupPolled((int)pin, mode, handler);
            }

            public static void Remove(WiringPiPinNumberEnum pin)
            {
                Setup(pin, InterruptModeEnum.None, null);
            }

            public static void Remove(BroadcomPinNumberEnum pin)
            {
                Setup(pin, InterruptModeEnum.None, null);
            }

            private static void InternalSetup(int pin, InterruptModeEnum mode, Action handler)
            {
                var h = handler != null ? new InterruptCallback(handler) : null;

                Unmanaged.Interrupts.InterruptServiceRoutine(pin, (int)mode, h);
            }

            // http://www-numi.fnal.gov/offline_software/srt_public_context/WebDocs/Errors/unix_system_errors.html
            private static IDisposable InternalSetupPolled(int pin, InterruptModeEnum mode, Action handler)
            {
                Unmanaged.Interrupts.InterruptServiceRoutine(pin, (int)mode, null);

                var thread = new Thread(o =>
                {
                    var token = (CancellationToken)o;

                    while (!token.IsCancellationRequested)
                    {
                        var result = Unmanaged.Interrupts.WaitForInterrupt(pin, 500);
                        switch (result)
                        {
                            case -1:
                                var error = Marshal.GetLastWin32Error();
                                if (error == 4) // EINTR            
                                    continue;

                                throw new Exception($"Error #{error} occured while waiting for interrupt.");

                            case -2:
                                throw new Exception("Pin descriptor not initialized.");

                            case 0:
                                break;

                            case 1:
                                handler();
                                break;

                            default:
                                throw new Exception($"Unrecognized result value '{result}' received from WaitForInterrupt .");
                        }
                    }
                });

                return new ThreadStopper(thread);
            }
        }

        public static class Spi
        {
            static Spi()
            {
                Init();
            }

            public static int Setup(SpiChannelEnum channel, int speed)
            {
                if (speed < 500000 || speed > 32000000)
                    throw new ArgumentOutOfRangeException(nameof(speed));

                var fileDescriptor = Unmanaged.Spi.Setup((int)channel, speed);
                if (fileDescriptor < 0)
                    switch (fileDescriptor)
                    {
                        case -1:
                            throw new Exception("Could not setup SPI. Function open() or ioctl() did not execute correctly.");

                        default:
                            throw new Exception($"Could not setup SPI. Result: {fileDescriptor}.");
                    }

                return fileDescriptor;
            }

            public static int Exchange(SpiChannelEnum channel, ref byte[] data, int? length = null)
            {
                var result = Unmanaged.Spi.Exchange((int)channel, data, length ?? data.Length);

                return result;
            }
        }

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public static class Serial
        {
            public enum BaudRateEnum
            {
                B50 = 50,
                B75 = 75,
                B110 = 110,
                B134 = 134,
                B150 = 150,
                B200 = 200,
                B300 = 300,
                B600 = 600,
                B1200 = 1200,
                B1800 = 1800,
                B2400 = 2400,
                B9600 = 9600,
                B192100 = 19200,
                B68400 = 38400,
                B57600 = 57600,
                B115200 = 115200,
                B230400 = 230400
            }

            static Serial()
            {
                Init();
            }

            private const string DeviceName = "/dev/ttyAMA0";
            private const BaudRateEnum Baudrate = BaudRateEnum.B115200;

            private static int? _descriptor;

            public static char Terminator { get; set; } = '\n';

            public static int Open(BaudRateEnum baudRate = Baudrate)
            {
                _descriptor = Unmanaged.Serial.Open(DeviceName, (int)baudRate);
                if (_descriptor.Value < 0)
                    switch (_descriptor.Value)
                    {
                        case -2:
                            throw new Exception($"Could not open Serial. Baud rate {baudRate} is not supported.");

                        case -1:
                            throw new Exception("Could not open Serial. Function open() did not execute correctly.");

                        default:
                            throw new Exception($"Could not open Serial. Result: {_descriptor.Value}.");
                    }

                return _descriptor.Value;
            }

            public static int GetAvailableDataCount()
            {
                return Unmanaged.Serial.DataAvailable(_descriptor.Value);
            }

            public static void Close()
            {
                Unmanaged.Serial.Close(_descriptor.Value);

                _descriptor = null;
            }

            public static char Get()
            {
                return (char)Unmanaged.Serial.Get(_descriptor.Value);
            }

            public static void Put(string input)
            {
                Unmanaged.Serial.Put(_descriptor.Value, input);

                Unmanaged.Serial.Put(_descriptor.Value, (byte)Terminator);
            }

            public static void Put(byte input)
            {
                Unmanaged.Serial.Put(_descriptor.Value, input);
            }

            public static void Flush()
            {
                Unmanaged.Serial.Flush(_descriptor.Value);
            }
        }
    }
}