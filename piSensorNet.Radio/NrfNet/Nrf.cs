using System;
using System.Linq;
using piSensorNet.Radio.NrfNet.Enums;
using piSensorNet.Radio.NrfNet.Registers;
using piSensorNet.WiringPi;
using piSensorNet.WiringPi.Enums;

namespace piSensorNet.Radio.NrfNet
{
    public class Nrf : IDisposable
    {
        public const int AddressSize = 5; // just for clarity, do not change
        public const int PayloadSize = 32; // just for clarity, do not change

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

        public Nrf(PinNumberEnum chipEnable, SpiChannelEnum channel, int spiSpeed = DefaultSpiSpeed)
        {
            _channel = channel;
            _chipEnable = chipEnable;

            Functionalities.Pins.Setup(_chipEnable, PinModeEnum.Output);
            Functionalities.Pins.Write(_chipEnable, false);

            Functionalities.Spi.Setup(_channel, spiSpeed);

            var general = (GeneralRegister)ReadRegister(RegisterEnum.General);
            var frequency = (FrequencyChannelRegister)ReadRegister(RegisterEnum.FrequencyChannel);

            State = general.PowerState;
            Mode = general.TransceiverMode;

            Frequency = frequency.Frequency;

            for (byte pipeNumber = 0; pipeNumber < 6; ++pipeNumber)
                WriteRegister(new PayloadSizeRegister(pipeNumber, PayloadSize));
        }

        public void SetupDelay()
            => Functionalities.Sleep.Milli(100);

        public StatusRegister WriteRegister(IRegister register)
            => WriteRegister(register.Type, register.Value);

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

                case RegisterEnum.General:
                    var general = (GeneralRegister)value;

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

        public byte ModifyRegister(RegisterEnum register, Func<byte, byte> action, out StatusRegister statusRegister)
        {
            var readValue = ReadRegister(register, out statusRegister);
            var setValue = action(readValue);

            if (setValue != readValue)
                statusRegister = WriteRegister(register, setValue);

            return setValue;
        }

        public byte ModifyRegister(RegisterEnum register, Func<byte, byte> action)
            => ModifyRegister(register, action, out _dummyStatusRegister);

        public TRegister ModifyRegister<TRegister>(RegisterEnum register, Action<TRegister> action, out StatusRegister statusRegister)
            where TRegister : IRegister
        {
            var readByteValue = ReadRegister(register, out statusRegister);
            var registerValue = (TRegister)(object)readByteValue;
            action(registerValue);
            var setByteValue = (byte)(object)registerValue;

            if (setByteValue != readByteValue)
                statusRegister = WriteRegister(register, setByteValue);

            return registerValue;
        }

        public TRegister ModifyRegister<TRegister>(RegisterEnum register, Action<TRegister> action)
            where TRegister : IRegister
            => ModifyRegister(register, action, out _dummyStatusRegister);

        public StatusRegister WriteCommand(CommandEnum command)
            => Exchange((byte)command, null);

        public StatusRegister Flush()
        {
            ClearInterrupts();

            WriteCommand(CommandEnum.FlushReceiveQueue);
            var status = WriteCommand(CommandEnum.FlushTransmitQueue);

            return status;
        }

        public StatusRegister ClearInterrupts()
            => WriteRegister(new StatusRegister(true, true, true, 0, false));

        public byte[] ReadPayload(byte[] buffer = null)
        {
            var payload = buffer ?? new byte[PayloadSize];

            Exchange((byte)CommandEnum.ReadReceivedPayload, payload);

            ClearInterrupts();

            return payload;
        }

        public bool WritePayload(byte[] payload, bool waitForSuccess, bool continuousOperation = false)
        {
            if (payload.Length > PayloadSize)
                throw new ArgumentException($"Payload size {payload.Length} is greater than maximum of {PayloadSize}.", nameof(payload));

            if (Mode != TransceiverModeEnum.Transmitter)
                ModifyRegister(RegisterEnum.General, i =>
                                                     {
                                                         var r = (GeneralRegister)i;

                                                         r.TransceiverMode = TransceiverModeEnum.Transmitter;

                                                         return r;
                                                     });

            // needed?
            WriteCommand(CommandEnum.FlushTransmitQueue); // something about sending this and below with READ instead of write, see datasheet

            var command = waitForSuccess ? CommandEnum.WriteTransmittedPayload : CommandEnum.WriteTransmittedPayloadWithoutAcknowledge;
            Exchange((byte)command, payload, true);

            Functionalities.Pins.Write(_chipEnable, true);

            // TX setting

            // Functionalities.Sleep.Micro(130);

            // TX mode (after Transmitter mode, data in TX FIF0, CE pulsed high)

            if (!waitForSuccess)
            {
                if (!continuousOperation)
                    Functionalities.Pins.Write(_chipEnable, false);

                return true;
            }

            StatusRegister status;
            while (!(status = ReadRegister(RegisterEnum.Status)).DataSent)
            {
                if (status.RetransmitLimitReached)
                {
                    WriteCommand(CommandEnum.FlushTransmitQueue);

                    Functionalities.Pins.Write(_chipEnable, false);

                    return false;
                }

                Functionalities.Sleep.Micro(50);
            }

            if (!continuousOperation)
                Functionalities.Pins.Write(_chipEnable, false);

            // Standby I

            return true;
        }

        public byte ReadPayloadSize(int pipeNumber)
        {
            if (0 > pipeNumber || pipeNumber > 5)
                throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            var pipeReceivedPayloadSizeRegister = (RegisterEnum)(byte)((byte)RegisterEnum.Pipe0PayloadSize + (byte)pipeNumber);

            var payloadSize = ReadRegister(pipeReceivedPayloadSizeRegister);

            return payloadSize;
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

            var bufferOut = (byte[])address;

            Exchange(writeTransmitAddress, bufferOut, true);

            if (withAcknowledge)
                SetAcknowledgeAddress(address);
        }

        public StatusRegister SetPipeReceiveAddress(byte pipeNumber, Address address)
        {
            if (pipeNumber >= 2)
                throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            var pipeRegister = pipeNumber.PipeAddressRegister();
            var writeReceiveAddress = (byte)((byte)CommandEnum.WriteRegister | (byte)pipeRegister);

            _pipeAddresses[pipeNumber] = address;

            var bufferOut = (byte[])address;

            var status = Exchange(writeReceiveAddress, bufferOut, true);

            return status;
        }

        public StatusRegister SetPipeReceiveAddress(byte pipeNumber, byte addressLeastSignificantByte)
        {
            if (pipeNumber < 2 || 5 < pipeNumber)
                throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            if (_pipeAddresses[1] == null)
                throw new InvalidOperationException("Address for pipe 1 must be set first.");

            _pipeAddresses[pipeNumber] = new Address(_pipeAddresses[1], addressLeastSignificantByte);

            var writeReceiveAddress = (RegisterEnum)((byte)CommandEnum.WriteRegister | (byte)pipeNumber.PipeAddressRegister());

            var status = WriteRegister(writeReceiveAddress, addressLeastSignificantByte);

            return status;
        }

        public void ConfigureInterrupt(PinNumberEnum pin, bool receiveInterruptEnabled,
            bool transmitInterruptEnabled, bool retransmitLimitReachedInterruptEnabled,
            Action<Nrf, byte?> handler)
        {
            ModifyRegister(RegisterEnum.General, i =>
                                                 {
                                                     var r = (GeneralRegister)i;

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
                ModifyRegister(RegisterEnum.General, i =>
                                                     {
                                                         var r = (GeneralRegister)i;

                                                         r.TransceiverMode = TransceiverModeEnum.Receiver;

                                                         return r;
                                                     });

            if (_pipeAddresses[0] != null && _pipeAddresses[0] != _lastAcknowledgeAddress)
                SetPipeReceiveAddress(0, _pipeAddresses[0]);
            else if (_isPipe0Enabled)
                ModifyRegister<ReceiverAddressRegister>(RegisterEnum.ReceiverAddress, register => register.EnableOnPipe0 = false);

            ChangePowerState(PowerStateEnum.Up);
            ClearInterrupts();

            Functionalities.Pins.Write(_chipEnable, true); // must be held high

            // RX setting

            Functionalities.Sleep.Micro(130);

            // RX mode

            // wai??
            if (((FeatureRegister)ReadRegister(RegisterEnum.Feature)).PayloadWithAcknowledgeEnabled)
                WriteCommand(CommandEnum.FlushTransmitQueue);
        }

        public void StopListening()
        {
            Functionalities.Pins.Write(_chipEnable, false);

            //Functionalities.Sleep.Micro(200);

            // Standby-I

            if (((FeatureRegister)ReadRegister(RegisterEnum.Feature)).PayloadWithAcknowledgeEnabled)
                WriteCommand(CommandEnum.FlushTransmitQueue);

            if (_isPipe0Enabled)
                ModifyRegister<ReceiverAddressRegister>(RegisterEnum.ReceiverAddress, register => register.EnableOnPipe0 = true);
        }

        public Address ReadPipeAddress(byte pipeNumber)
        {
            if (pipeNumber > 5)
                throw new ArgumentOutOfRangeException(nameof(pipeNumber));

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

        public Address GetPipeAddress(byte pipeNumber)
        {
            if (pipeNumber > 5)
                throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            return _pipeAddresses[pipeNumber];
        }

        public StatusRegister ChangePowerState(PowerStateEnum state)
            => ModifyRegister(RegisterEnum.General, register =>
                                                    {
                                                        var generalRegister = (GeneralRegister)register;

                                                        generalRegister.PowerState = state;

                                                        return generalRegister;
                                                    });

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

        private StatusRegister Exchange(byte registerOrCommand, byte[] data, bool discardResponse = false)
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

        private void SetAcknowledgeAddress(Address address)
        {
            if (address == _lastAcknowledgeAddress)
                return;

            _lastAcknowledgeAddress = address;

            SetPipeReceiveAddress(0, address);
        }

        public void Dispose()
        {
            Functionalities.Pins.Write(_chipEnable, false);
        }
    }
}