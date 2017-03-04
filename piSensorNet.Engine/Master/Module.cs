using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using piSensorNet.Radio.NrfNet;
using piSensorNet.Radio.NrfNet.Enums;
using piSensorNet.Radio.NrfNet.Registers;
using piSensorNet.WiringPi.Enums;

namespace piSensorNet.Engine.Master
{
    public class Module : IDisposable
    {
        private readonly bool _sendContinuously;

        private readonly BackgroundWorker _worker;
        private readonly Nrf _nrf;

        public event Action<byte[]> Received;

        public Address Address { get; }
        public Address BroadcastAddress { get; }
        public ConcurrentQueue<Message> SendQueue { get; } = new ConcurrentQueue<Message>();

        public Module(PinNumberEnum chipEnable, SpiChannelEnum channel, Address address, Address broadcastAddress, byte radioChannel, bool sendContinuously = false, int spiSpeed = Nrf.DefaultSpiSpeed)
        {
            Address = address;
            BroadcastAddress = broadcastAddress;

            _sendContinuously = sendContinuously;
            _nrf = new Nrf(chipEnable, channel, spiSpeed);

            _nrf.WriteRegister(new GeneralRegister(false, false, false, true, CrcLengthEnum.TwoBytes, PowerStateEnum.Down, TransceiverModeEnum.Receiver));
            _nrf.WriteRegister(new AutoAcknowledgmentRegister(true, true, true, true, true, true));
            _nrf.WriteRegister(new ReceiverAddressRegister(true, true, false, false, false, false));
            _nrf.WriteRegister(new AddressWidthRegister(AddressWidthEnum.FiveBytes));
            _nrf.WriteRegister(new AutoRetransmissionRegister(AutoRetransmitDelayEnum.T1000us, 3));
            _nrf.WriteRegister(new FrequencyChannelRegister(radioChannel));
            _nrf.WriteRegister(new RadioRegister(false, false, DataRateEnum.S1Mbs, OutputPowerEnum.Pminus6dbm));
            _nrf.WriteRegister(new FeatureRegister(false, false, true));
            _nrf.WriteRegister(new DynamicPayloadLengthRegister(false, false, false, false, false, false));

            _nrf.Flush();

            _nrf.SetPipeReceiveAddress(1, Address);

            _nrf.WriteRegister(RegisterEnum.Pipe0PayloadSize, Nrf.PayloadSize);
            _nrf.WriteRegister(RegisterEnum.Pipe1PayloadSize, Nrf.PayloadSize);

            _nrf.ModifyRegister<GeneralRegister>(RegisterEnum.General, register => register.TransceiverMode = TransceiverModeEnum.Transmitter);

            _nrf.SetupDelay();

            _nrf.ChangePowerState(PowerStateEnum.Up);

            // Standby-I

            _worker = new BackgroundWorker
                      {
                          WorkerSupportsCancellation = true,
                      };

            _worker.DoWork += Loop;
        }

        public void Start()
            => _worker.RunWorkerAsync(this);

        public void Stop()
            => _worker.CancelAsync();

        private static void Loop(object sender, DoWorkEventArgs args)
        {
            var worker = (BackgroundWorker)sender;
            var module = (Module)args.Argument;

            module._nrf.StartListening();

            while (!worker.CancellationPending)
            {
                Message message;

                if (!worker.CancellationPending && module.SendQueue.TryDequeue(out message))
                {
                    module._nrf.StopListening();

                    do
                    {
                        module._nrf.SetTransmitAddress(message.Recipient, message.WaitForAcknowledge);
                        module._nrf.WritePayload(message.Payload, message.WaitForAcknowledge);
                    } while (!worker.CancellationPending && module._sendContinuously && module.SendQueue.TryDequeue(out message));

                    module._nrf.StartListening();
                }

                if (!worker.CancellationPending && module._nrf.IsDataAvailable())
                {
                    var payload = module._nrf.ReadPayload();

                    module.Received?.Invoke(payload);
                }
            }

            args.Cancel = true;
        }

        public void Dispose()
        {
            _nrf.Dispose();
        }
    }
}