using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using piSensorNet.Common.System;
using piSensorNet.WiringPi.Enums;
using piSensorNet.WiringPi.Unmanaged;

namespace piSensorNet.WiringPi
{
    public static class Functionalities
    {
        private static readonly bool IsInitialized;

        static Functionalities()
        {
            IsInitialized = true;

            var result = Configuration.Setup();
            if (result == 0)
                return;

            switch (result)
            {
                case 1:
                    throw new Exception("Could not setup wiringPi. Run program with superuser permissions.");

                default:
                    throw new Exception("Could not setup wiringPi.");
            }
        }

        private static void Init()
        {
            if (!IsInitialized)
                RuntimeHelpers.RunClassConstructor(typeof(Functionalities).TypeHandle);
        }

        public static class Pins
        {
            static Pins()
            {
                Init();
            }

            public static void Setup(PinNumberEnum pin, PinModeEnum mode, PullUpModeEnum? pullUpMode = null)
                => InternalSetup((int)pin, mode, pullUpMode);

            public static void Setup(BroadcomPinNumberEnum pin, PinModeEnum mode, PullUpModeEnum? pullUpMode = null)
                => InternalSetup((int)pin, mode, pullUpMode);

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

            public static void Write(PinNumberEnum pin, bool state)
                => InternalWrite((int)pin, state);

            public static void Write(BroadcomPinNumberEnum pin, bool state)
                => InternalWrite((int)pin, state);

            private static void InternalWrite(int pin, bool state)
                => Unmanaged.Pins.DigitalWrite(pin, Convert.ToInt32(state));

            public static bool Read(PinNumberEnum pin)
                => InternalRead((int)pin);

            public static bool Read(BroadcomPinNumberEnum pin)
                => InternalRead((int)pin);

            private static bool InternalRead(int pin)
                => Unmanaged.Pins.DigitalRead(pin) > 0;
        }

        public static class Interrupts
        {
            static Interrupts()
            {
                Init();
            }

            public static void Setup(PinNumberEnum pin, InterruptModeEnum mode, Action handler)
                => InternalSetup((int)pin, mode, handler);

            public static void Setup(BroadcomPinNumberEnum pin, InterruptModeEnum mode, Action handler)
                => InternalSetup((int)pin, mode, handler);

            public static IDisposable SetupPolled(PinNumberEnum pin, InterruptModeEnum mode, Action handler)
                => InternalSetupPolled((int)pin, mode, handler);

            public static IDisposable SetupPolled(BroadcomPinNumberEnum pin, InterruptModeEnum mode, Action handler)
                => InternalSetupPolled((int)pin, mode, handler);

            public static void Remove(PinNumberEnum pin)
                => Setup(pin, InterruptModeEnum.None, null);

            public static void Remove(BroadcomPinNumberEnum pin)
                => Setup(pin, InterruptModeEnum.None, null);

            private static void InternalSetup(int pin, InterruptModeEnum mode, Action handler)
            {
                var callback = handler != null ? new InterruptCallback(handler) : null;

                Unmanaged.Interrupts.InterruptServiceRoutine(pin, (int)mode, callback);
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

                var spiPin = channel.ToPinNumber();

                Pins.Setup(spiPin, PinModeEnum.Output);
                Pins.Write(spiPin, true);

                var fileDescriptor = Unmanaged.Spi.Setup((int)channel, speed);
                if (fileDescriptor >= 0)
                    return fileDescriptor;

                switch (fileDescriptor)
                {
                    case -1:
                        throw new Exception("Could not setup SPI. Function open() or ioctl() did not execute correctly.");

                    default:
                        throw new Exception($"Could not setup SPI. Result: {fileDescriptor}.");
                }
            }

            public static int Exchange(SpiChannelEnum channel, byte[] data, int? length = null)
                => Unmanaged.Spi.Exchange((int)channel, data, length ?? data.Length);
        }

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public static class Serial
        {
            static Serial()
            {
                Init();
            }

            private const string DeviceName = "/dev/ttyAMA0";
            private const SerialBaudRateEnum Baudrate = SerialBaudRateEnum.B115200;

            private static int? _descriptor;

            public static char Terminator { get; set; } = '\n';

            public static int Open(SerialBaudRateEnum baudRate = Baudrate)
            {
                if (_descriptor.HasValue)
                    throw new InvalidOperationException("Serial is already opened.");

                _descriptor = Unmanaged.Serial.Open(DeviceName, (int)baudRate);

                if (_descriptor.Value >= 0)
                    return _descriptor.Value;

                switch (_descriptor.Value)
                {
                    case -2:
                        throw new Exception($"Could not open Serial. Baud rate {baudRate} is not supported.");

                    case -1:
                        throw new Exception("Could not open Serial. Function open() did not execute correctly.");

                    default:
                        throw new Exception($"Could not open Serial. Result: {_descriptor.Value}.");
                }
            }

            public static int GetAvailableDataCount()
            {
                if (!_descriptor.HasValue)
                    throw new InvalidOperationException("Serial is not opened. Call Open() method first.");

                return Unmanaged.Serial.DataAvailable(_descriptor.Value);
            }

            public static void Close()
            {
                if (!_descriptor.HasValue)
                    throw new InvalidOperationException("Serial is not opened. Call Open() method first.");

                Unmanaged.Serial.Close(_descriptor.Value);

                _descriptor = null;
            }

            public static char Get()
            {
                if (!_descriptor.HasValue)
                    throw new InvalidOperationException("Serial is not opened. Call Open() method first.");

                return (char)Unmanaged.Serial.Get(_descriptor.Value);
            }

            public static void Put(string input)
            {
                if (!_descriptor.HasValue)
                    throw new InvalidOperationException("Serial is not opened. Call Open() method first.");

                Unmanaged.Serial.Put(_descriptor.Value, input);
                Unmanaged.Serial.Put(_descriptor.Value, (byte)Terminator);
            }

            public static void Put(byte input)
            {
                if (!_descriptor.HasValue)
                    throw new InvalidOperationException("Serial is not opened. Call Open() method first.");

                Unmanaged.Serial.Put(_descriptor.Value, input);
            }

            public static void Flush()
            {
                if (!_descriptor.HasValue)
                    throw new InvalidOperationException("Serial is not opened. Call Open() method first.");

                Unmanaged.Serial.Flush(_descriptor.Value);
            }
        }

        public static class Sleep
        {
            static Sleep()
            {
                Init();
            }

            public static void Micro(uint microseconds)
                => Unmanaged.Sleep.Micro(microseconds);

            public static void Milli(uint milliseconds)
                => Unmanaged.Sleep.Milli(milliseconds);
        }
    }
}