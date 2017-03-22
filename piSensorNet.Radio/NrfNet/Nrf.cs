using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;
using piSensorNet.Radio.NrfNet.Enums;
using piSensorNet.Radio.NrfNet.Registers;
using piSensorNet.WiringPi;
using piSensorNet.WiringPi.Enums;

namespace piSensorNet.Radio.NrfNet
{
    public sealed class Nrf : IDisposable
    {
        public const int AddressSize = 5;
        public const int PayloadSize = 32;

        public const int DefaultSpiSpeed = 8000000;

        private const byte NOP = (byte)CommandEnum.NoOperation;

        private readonly byte[] _tinyBuffer = new byte[1];
        private readonly byte[] _exchangeBuffer = new byte[PayloadSize + 1];
        private readonly Address[] _pipeAddresses = new Address[6];

        private readonly SpiChannelEnum _channel;
        private Address _lastTransmitAddress;
        private Address _lastAcknowledgeAddress;
        private readonly PinNumberEnum _chipEnable;
        private StatusRegister _dummyStatusRegister;
        private bool _isPipe0Enabled;

        public PowerStateEnum State { get; private set; }
        public TransceiverModeEnum Mode { get; private set; }
        public ulong Frequency { get; private set; }

        public bool IsConnected => !Exchange(NOP, null).IsEqualTo(byte.MinValue, byte.MaxValue);

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static void Debug(string text)
            => Console.WriteLine($"### {DateTime.Now.ToFullTimeString()}: {text}");

        public Nrf(PinNumberEnum chipEnable, SpiChannelEnum channel, int spiSpeed = DefaultSpiSpeed)
        {
            _channel = channel;
            _chipEnable = chipEnable;

            Functionalities.Pins.Setup(_chipEnable, PinModeEnum.Output);
            Functionalities.Pins.Write(_chipEnable, false);

            Functionalities.Spi.Setup(_channel, spiSpeed);

            Functionalities.Sleep.Milli(5);

            var general = (ConfigurationRegister)ReadRegister(RegisterEnum.Configuration);
            var frequency = (FrequencyChannelRegister)ReadRegister(RegisterEnum.FrequencyChannel);

            State = general.PowerState;
            Mode = general.TransceiverMode;

            Frequency = frequency.Frequency;

            for (byte pipeNumber = 0; pipeNumber < 6; ++pipeNumber)
                WriteRegister(new PayloadSizeRegister(pipeNumber, PayloadSize));
        }

        public void SetupDelay()
            => Functionalities.Sleep.Milli(100);

        [NotNull]
        public StatusRegister WriteRegister([NotNull] IRegister register)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));

            return WriteRegister(register.Type, register.Value);
        }

        [NotNull]
        public StatusRegister WriteRegister(RegisterEnum register, byte value)
        {
            _tinyBuffer[0] = value;

            var registerOrCommand = (byte)((byte)register | (byte)CommandEnum.WriteRegister);
            var status = Exchange(registerOrCommand, _tinyBuffer, true);

            switch (register)
            {
                case RegisterEnum.FrequencyChannel:
                    Frequency = ((FrequencyChannelRegister)value).Frequency;
                    break;

                case RegisterEnum.Configuration:
                    var general = (ConfigurationRegister)value;

                    if (State != general.PowerState) // transition
                        if (State == PowerStateEnum.Down) // from down to up
                            Functionalities.Sleep.Milli(5);
                        else if (State == PowerStateEnum.Up) // from up to down
                            Functionalities.Pins.Write(_chipEnable, false);

                    State = general.PowerState;
                    Mode = general.TransceiverMode;

                    break;

                case RegisterEnum.ReceiverAddress:
                    _isPipe0Enabled = ((ReceiverAddressRegister)value).EnableOnPipe0;
                    break;
            }

            return status;
        }

        public byte ReadRegister(RegisterEnum register, out StatusRegister statusRegister)
        {
            var registerOrCommand = (byte)((byte)register | (byte)CommandEnum.ReadRegister);

            if (register == RegisterEnum.Status)
            {
                statusRegister = Exchange(NOP, null);

                return statusRegister;
            }

            _tinyBuffer[0] = NOP;

            statusRegister = Exchange(registerOrCommand, _tinyBuffer);

            return _tinyBuffer[0];
        }

        public byte ReadRegister(RegisterEnum register)
            => ReadRegister(register, out _dummyStatusRegister);

        public byte ModifyRegister(RegisterEnum register, [NotNull] Func<byte, byte> action, out StatusRegister statusRegister)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var readValue = ReadRegister(register, out statusRegister);
            var setValue = action(readValue);

            if (setValue != readValue)
                statusRegister = WriteRegister(register, setValue);

            return setValue;
        }

        public byte ModifyRegister(RegisterEnum register, Func<byte, byte> action)
            => ModifyRegister(register, action, out _dummyStatusRegister);

        [NotNull]
        public TRegister ModifyRegister<TRegister>(RegisterEnum register, [NotNull] Action<TRegister> action, out StatusRegister statusRegister)
            where TRegister : IRegister
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var readByteValue = ReadRegister(register, out statusRegister);
            var registerValue = (TRegister)(dynamic)readByteValue;
            action(registerValue);
            var setByteValue = (byte)(dynamic)registerValue;

            if (setByteValue != readByteValue)
                statusRegister = WriteRegister(register, setByteValue);

            return registerValue;
        }

        [NotNull]
        public TRegister ModifyRegister<TRegister>(RegisterEnum register, Action<TRegister> action)
            where TRegister : IRegister
            => ModifyRegister(register, action, out _dummyStatusRegister);

        [NotNull]
        public StatusRegister WriteCommand(CommandEnum command)
            => Exchange((byte)command, null);

        [NotNull]
        public StatusRegister Flush()
        {
            ClearInterrupts();

            WriteCommand(CommandEnum.FlushReceiveQueue);

            return WriteCommand(CommandEnum.FlushTransmitQueue);
        }

        [NotNull]
        public StatusRegister ClearInterrupts()
            => WriteRegister(new StatusRegister(true, true, true));

        [NotNull]
        public byte[] ReadPayload([CanBeNull] byte[] buffer = null)
        {
            var payload = buffer ?? new byte[PayloadSize];

            Exchange((byte)CommandEnum.ReadPayload, payload);

            ClearInterrupts();

            return payload;
        }

        public bool WritePayload([NotNull] byte[] payload, bool withAcknowledge, bool readRetransmissionsCount, out byte retransmissionsCount)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Length > PayloadSize) throw new ArgumentException($"Payload size {payload.Length} is greater than maximum of {PayloadSize}.", nameof(payload));

            if (Mode != TransceiverModeEnum.Transmitter)
                ModifyRegister(RegisterEnum.Configuration, i =>
                                                           {
                                                               var r = (ConfigurationRegister)i;

                                                               r.TransceiverMode = TransceiverModeEnum.Transmitter;

                                                               return r;
                                                           });

            var command = withAcknowledge ? CommandEnum.WritePayload : CommandEnum.WritePayloadNoAcknowledge;
            Exchange((byte)command, payload, true);

            Functionalities.Pins.Write(_chipEnable, true);
            Functionalities.Sleep.Micro(15);
            Functionalities.Pins.Write(_chipEnable, false);

            // TX setting

            // TX mode (after Transmitter mode, data in TX FIF0, CE pulsed high)

            StatusRegister status;
            while (!((status = ReadRegister(RegisterEnum.Status)).DataSent || status.RetransmitLimitReached)) {}

            // Standby I

            status = ClearInterrupts();
            if (status.RetransmitLimitReached)
            {
                WriteCommand(CommandEnum.FlushTransmitQueue);

                retransmissionsCount = byte.MaxValue;

                return false;
            }

            if (!readRetransmissionsCount)
            {
                retransmissionsCount = 0;
                return true;
            }

            var observeRegister = (TransmitObserveRegister)ReadRegister(RegisterEnum.TransmitObserve);
            retransmissionsCount = observeRegister.RetransmittedPacketCount;

            return true;
        }

        public byte ReadPayloadSize(byte pipeNumber)
        {
            if (pipeNumber > 5) throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            var pipePayloadSizeRegister = pipeNumber.PipePayloadSizeRegister();

            return ReadRegister(pipePayloadSizeRegister);
        }

        public void SetTransmitAddress(Address address, bool withAcknowledge)
        {
            const byte writeTransmitAddress = (byte)CommandEnum.WriteRegister | (byte)RegisterEnum.TransmitAddress;

            if (address == _lastTransmitAddress)
            {
                if (withAcknowledge)
                    SetAcknowledgeAddress(address);

                return;
            }

            _lastTransmitAddress = address;

            if (withAcknowledge)
                SetAcknowledgeAddress(address);

            Exchange(writeTransmitAddress, address.Bytes, true);
        }

        [NotNull]
        public StatusRegister SetPipeReceiveAddress(byte pipeNumber, [NotNull] Address address)
            => InternalSetPipeReceiveAddress(pipeNumber, address, true);

        [NotNull]
        public StatusRegister SetPipeReceiveAddress(byte pipeNumber, byte addressLeastSignificantByte)
        {
            if (pipeNumber < 2 || 5 < pipeNumber) throw new ArgumentOutOfRangeException(nameof(pipeNumber));
            if (_pipeAddresses[1] == null) throw new InvalidOperationException("Address for pipe 1 must be set first.");

            _pipeAddresses[pipeNumber] = new Address(_pipeAddresses[1], addressLeastSignificantByte);

            var pipeAddressRegister = pipeNumber.PipeAddressRegister();
            var writeReceiveAddress = (RegisterEnum)((byte)CommandEnum.WriteRegister | (byte)pipeAddressRegister);

            return WriteRegister(writeReceiveAddress, addressLeastSignificantByte);
        }

        public void ConfigureInterrupt(PinNumberEnum pin, bool receiveInterruptEnabled,
            bool transmitInterruptEnabled, bool retransmitLimitReachedInterruptEnabled,
            [NotNull] Action<Nrf, byte?> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            ModifyRegister(RegisterEnum.Configuration, i =>
                                                       {
                                                           var r = (ConfigurationRegister)i;

                                                           r.ReceiveInterruptEnabled = receiveInterruptEnabled;
                                                           r.TransmitInterruptEnabled = transmitInterruptEnabled;
                                                           r.RetransmitLimitReachedInterruptEnabled = retransmitLimitReachedInterruptEnabled;

                                                           return r;
                                                       });

            Functionalities.Pins.Setup(pin, PinModeEnum.Input, PullUpModeEnum.Up);
            Functionalities.Interrupts.Setup(pin, InterruptModeEnum.FallingEdge, () =>
                                                                                 {
                                                                                     var status = (StatusRegister)ReadRegister(RegisterEnum.Status);
                                                                                     handler(this, status.DataReadyPipeNumber);
                                                                                 });
        }

        public void StartListening()
        {
            if (Mode != TransceiverModeEnum.Receiver)
                ModifyRegister(RegisterEnum.Configuration, i =>
                                                           {
                                                               var r = (ConfigurationRegister)i;

                                                               r.TransceiverMode = TransceiverModeEnum.Receiver;

                                                               return r;
                                                           });

            ClearInterrupts();

            Functionalities.Pins.Write(_chipEnable, true);

            if (_pipeAddresses[0] != null)
            {
                _lastAcknowledgeAddress = null; // have to clear it, so next time when set, it will actually set
                InternalSetPipeReceiveAddress(0, _pipeAddresses[0], true);
            }
            else if (!_isPipe0Enabled)
                ModifyRegister<ReceiverAddressRegister>(RegisterEnum.ReceiverAddress, register => register.EnableOnPipe0 = false);

            ChangePowerState(PowerStateEnum.Up);

            // RX setting

            // takes about 130us

            // RX mode

            if (((FeatureRegister)ReadRegister(RegisterEnum.Feature)).AcknowledgeWithPayloadEnabled)
                WriteCommand(CommandEnum.FlushTransmitQueue);
        }

        public void StopListening()
        {
            Functionalities.Pins.Write(_chipEnable, false);
            Functionalities.Sleep.Micro(200);

            // Standby-I

            //if (((FeatureRegister)ReadRegister(RegisterEnum.Feature)).AcknowledgeWithPayloadEnabled)
            //{
            //    Functionalities.Sleep.Micro(200);
            //    WriteCommand(CommandEnum.FlushTransmitQueue);
            //}

            if (_isPipe0Enabled)
                ModifyRegister<ReceiverAddressRegister>(RegisterEnum.ReceiverAddress, register => register.EnableOnPipe0 = true);
        }

        public Address ReadPipeAddress(byte pipeNumber)
        {
            if (pipeNumber > 5) throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            var pipeRegister = pipeNumber.PipeAddressRegister();
            var readReceiveAddress = (byte)((byte)CommandEnum.ReadRegister | (byte)pipeRegister);
            switch (pipeNumber)
            {
                case 0:
                case 1:
                {
                    var address = new Address();

                    Exchange(readReceiveAddress, address.Bytes);

                    return address;
                }

                default:
                {
                    var address = ReadPipeAddress(1);

                    _tinyBuffer[0] = NOP;

                    Exchange(readReceiveAddress, _tinyBuffer);

                    address.SetLeastSignificantByte(_tinyBuffer[0]);

                    return address;
                }
            }
        }

        [CanBeNull]
        public Address GetPipeAddress(byte pipeNumber)
        {
            if (pipeNumber > 5) throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            return _pipeAddresses[pipeNumber];
        }

        [NotNull]
        public StatusRegister ChangePowerState(PowerStateEnum state)
        {
            if (state == PowerStateEnum.Down)
                Functionalities.Pins.Write(_chipEnable, false);

            return ModifyRegister(RegisterEnum.Configuration, register =>
                                                              {
                                                                  var generalRegister = (ConfigurationRegister)register;

                                                                  generalRegister.PowerState = state;

                                                                  return generalRegister;
                                                              });
        }

        public bool IsDataAvailable()
            => !((FifoStatusRegister)ReadRegister(RegisterEnum.FifoStatus)).ReceiveQueueEmpty;

        public bool IsDataAvailable(out byte pipeNumber)
        {
            var isDataAvailable = IsDataAvailable();

            if (!isDataAvailable)
            {
                pipeNumber = 0;

                return false;
            }

            var dataReadyPipeNumber = ((StatusRegister)ReadRegister(RegisterEnum.Status)).DataReadyPipeNumber;
            if (!dataReadyPipeNumber.HasValue)
                throw new Exception("Data was ready but no pipe number provided.");

            pipeNumber = dataReadyPipeNumber.Value;

            return true;
        }

        public void Dispose()
        {
            Functionalities.Pins.Write(_chipEnable, false);
            ChangePowerState(PowerStateEnum.Down);
        }

        private byte Exchange(byte registerOrCommand, [CanBeNull] byte[] data, bool discardResponse = false)
        {
            var length = data?.Length ?? 0;
            var bufferLength = length + 1;

            if (data == null)
                discardResponse = true;

            _exchangeBuffer[0] = registerOrCommand;

            if (length > 0)
                data?.CopyTo(_exchangeBuffer, 1);

            Functionalities.Spi.Exchange(_channel, _exchangeBuffer, bufferLength);

            if (!discardResponse)
                Array.Copy(_exchangeBuffer, 1, data, 0, length);

            return _exchangeBuffer[0];
        }

        [NotNull]
        private StatusRegister InternalSetPipeReceiveAddress(byte pipeNumber, [NotNull] Address address, bool saveAddress)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (pipeNumber >= 2) throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            var pipeAddressRegister = pipeNumber.PipeAddressRegister();
            var writeReceiveAddress = (byte)((byte)CommandEnum.WriteRegister | (byte)pipeAddressRegister);

            if (saveAddress)
                _pipeAddresses[pipeNumber] = address;

            return Exchange(writeReceiveAddress, address.Bytes, true);
        }

        private void SetAcknowledgeAddress([NotNull] Address address)
        {
            if (address == _lastAcknowledgeAddress)
                return;

            _lastAcknowledgeAddress = address;

            InternalSetPipeReceiveAddress(0, address, false);
        }
    }
}