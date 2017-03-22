using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;
using piSensorNet.Radio.NrfNet;
using piSensorNet.Radio.NrfNet.Enums;
using piSensorNet.Radio.NrfNet.Registers;
using piSensorNet.WiringPi.Enums;

namespace piSensorNet.Engine.Master
{
    public sealed class Module : IDisposable
    {
        private bool _isWorkerRunning;
        private readonly bool _sendContinuously;
        private readonly bool _readRetransmissionsCount;

        private readonly BackgroundWorker _worker;
        private readonly Nrf _nrf;
        private readonly ConcurrentQueue<Message> _priorityQueue = new ConcurrentQueue<Message>();

        public event EventHandler<PayloadReceivedEventArgs> Received;
        public event EventHandler<PayloadSentEventArgs> Sent;
        public event EventHandler<PayloadSendingFailedEventArgs> SendingFailed;
        public event EventHandler<ErrorEventArgs> Error;

        public Address Address { get; }
        public Address BroadcastAddress { get; }
        public ConcurrentQueue<Message> SendQueue { get; } = new ConcurrentQueue<Message>();

        private static void Debug(string text)
            => Console.WriteLine($"### {DateTime.Now.ToFullTimeString()}: {text}");

        public Module(PinNumberEnum chipEnable, SpiChannelEnum channel, [NotNull] Address address, [NotNull] Address broadcastAddress, byte radioChannel,
            bool sendContinuously = false, bool readRetransmissionsCount = false,
            OutputPowerEnum outputPower = OutputPowerEnum.P0dbm, DataRateEnum dataRate = DataRateEnum.S1Mbps,
            byte autoRetransmitRetryLimit = 10, AutoRetransmitDelayEnum autoRetransmitDelay = AutoRetransmitDelayEnum.T2000us,
            int spiSpeed = Nrf.DefaultSpiSpeed)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (broadcastAddress == null) throw new ArgumentNullException(nameof(broadcastAddress));

            Packet.BaseAddress = address;

            Address = address;
            BroadcastAddress = broadcastAddress;

            _sendContinuously = sendContinuously;
            _readRetransmissionsCount = readRetransmissionsCount;

            _nrf = new Nrf(chipEnable, channel, spiSpeed);

            if (!_nrf.IsConnected)
                throw new InvalidOperationException("NRF doesn't seem to be connected.");

            _nrf.WriteRegister(new ConfigurationRegister(false, false, false, true, CrcLengthEnum.TwoBytes, PowerStateEnum.Down, TransceiverModeEnum.Receiver));
            _nrf.WriteRegister(new AutoAcknowledgmentRegister(true, true, false, false, false, false));
            _nrf.WriteRegister(new ReceiverAddressRegister(true, true, false, false, false, false));
            _nrf.WriteRegister(new AddressWidthRegister(AddressWidthEnum.FiveBytes));
            _nrf.WriteRegister(new AutoRetransmissionRegister(autoRetransmitDelay, autoRetransmitRetryLimit));
            _nrf.WriteRegister(new FrequencyChannelRegister(radioChannel));
            _nrf.WriteRegister(new RadioRegister(false, false, dataRate, outputPower));
            _nrf.WriteRegister(new FeatureRegister(false, false, true));
            _nrf.WriteRegister(new DynamicPayloadLengthRegister(false, false, false, false, false, false));

            _nrf.Flush();

            _nrf.SetPipeReceiveAddress(1, Address);

            _nrf.ModifyRegister<ConfigurationRegister>(RegisterEnum.Configuration, register => register.TransceiverMode = TransceiverModeEnum.Transmitter);

            _nrf.SetupDelay();

            _nrf.ChangePowerState(PowerStateEnum.Up);

            // Standby-I

            _worker = new BackgroundWorker
                      {
                          WorkerSupportsCancellation = true,
                      };

            _worker.DoWork += Work;
            _worker.RunWorkerCompleted += WorkCompleted;
        }

        public void Start(bool waitForWorker)
        {
            if (Received == null)
                throw new InvalidOperationException($"Event {nameof(Received)} has no listeners. Attach at least one before starting.");

            if (_worker.IsBusy)
                throw new InvalidOperationException("Module is already started.");

            _worker.RunWorkerAsync(this);

            while (waitForWorker && !_isWorkerRunning)
                Thread.Sleep(10);
        }

        public void Stop()
        {
            if (!_worker.IsBusy)
                throw new InvalidOperationException("Module is already stopped.");

            _worker.CancelAsync();
        }

        public void EnqueueForSending(Message message)
            => SendQueue.Enqueue(message);

        public void Dispose()
        {
            if (_worker.IsBusy)
                _worker.CancelAsync();

            _nrf.Dispose();
        }

        private static void Work(object sender, DoWorkEventArgs args)
        {
            Debug("Starting worker loop...");

            var worker = (BackgroundWorker)sender;
            var module = (Module)args.Argument;

            args.Result = module;

            module._nrf.StartListening();

            module._isWorkerRunning = true;

            while (!worker.CancellationPending)
            {
                Message message;
                byte retransmissionsCount;

                if (!worker.CancellationPending && module._nrf.IsDataAvailable())
                {
                    var payload = module._nrf.ReadPayload();

                    Debug("Received payload.");
                    module.Received?.Invoke(module, new PayloadReceivedEventArgs(payload, DateTime.UtcNow));
                }

                if (!worker.CancellationPending && module._priorityQueue.TryDequeue(out message))
                {
                    module._nrf.StopListening();

                    do
                    {
                        Debug("Dequeued priority message. Transmitting...");

                        module._nrf.SetTransmitAddress(message.Recipient, message.WithAcknowledge);

                        if (module._nrf.WritePayload(message.Payload, message.WithAcknowledge, module._readRetransmissionsCount, out retransmissionsCount))
                        {
                            Debug("Sent payload.");
                            module.Sent?.Invoke(module, new PayloadSentEventArgs(message, retransmissionsCount, DateTime.UtcNow));
                        }
                        else
                        {
                            Debug("Failed to sent payload.");
                            var sendingFailedEventArgs = new PayloadSendingFailedEventArgs(message, false, DateTime.UtcNow);
                            module.SendingFailed?.Invoke(module, sendingFailedEventArgs);

                            if (!sendingFailedEventArgs.Requeue)
                                continue;

                            module._priorityQueue.Enqueue(message);
                            break;
                        }
                    } while (!worker.CancellationPending && module._sendContinuously && module._priorityQueue.TryDequeue(out message));

                    module._nrf.StartListening();
                }
                else if (!worker.CancellationPending && module.SendQueue.TryDequeue(out message))
                {
                    module._nrf.StopListening();

                    do
                    {
                        Debug("Dequeued message. Transmitting...");

                        module._nrf.SetTransmitAddress(message.Recipient, message.WithAcknowledge);

                        if (module._nrf.WritePayload(message.Payload, message.WithAcknowledge, module._readRetransmissionsCount, out retransmissionsCount))
                        {
                            Debug("Sent payload.");
                            module.Sent?.Invoke(module, new PayloadSentEventArgs(message, retransmissionsCount, DateTime.UtcNow));
                        }
                        else
                        {
                            Debug("Failed to sent payload.");
                            var sendingFailedEventArgs = new PayloadSendingFailedEventArgs(message, true, DateTime.UtcNow);
                            module.SendingFailed?.Invoke(module, sendingFailedEventArgs);

                            if (!sendingFailedEventArgs.Requeue)
                                continue;

                            module._priorityQueue.Enqueue(message);
                            break;
                        }
                    } while (!worker.CancellationPending && module._sendContinuously && module.SendQueue.TryDequeue(out message));

                    module._nrf.StartListening();
                }
            }

            module._isWorkerRunning = false;
            Debug("Exiting worker loop...");
        }

        private static void WorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            Debug("Worker finished.");
            var module = (Module)args.Result;

            if (args.Error != null)
                module.Error?.Invoke(module, new ErrorEventArgs(args.Error));
        }
    }
}