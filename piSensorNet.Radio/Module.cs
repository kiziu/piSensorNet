/*
using System;
using System.Linq;
using piSensorNet.Common.Custom;
using piSensorNet.Common.System;
using piSensorNet.Radio.Configuration;
using piSensorNet.Radio.NrfNet;
using piSensorNet.Radio.NrfNet.Enums;
using piSensorNet.Radio.NrfNet.Registers;

namespace piSensorNet.Radio
{
    internal static class Module
    {
        public static IRadioConfiguration Configuration { get; } = ReadOnlyConfiguration.Load<IRadioConfiguration>("config.json");
        private static readonly Address BroadcastAddress = new Address(Configuration.BroadcastAddress);
        private static readonly Address HubAddress = new Address(Configuration.HubAddress);

        private static bool _loop = true;

        internal static int Main1(string[] args)
        {
            Signal.BindHandler(SignalHandler, SignalTypeEnum.Interrupt, SignalTypeEnum.Terminate);

            Console.WriteLine("Setting up radio...");

            var nrf = new Nrf(Configuration.ChipEnablePin, Configuration.SpiChannel);
            
            nrf.WriteRegister(new GeneralRegister(false, false, false, true, CrcLengthEnum.TwoBytes, PowerStateEnum.Down, TransceiverModeEnum.Receiver));
            nrf.WriteRegister(new AutoAcknowledgmentRegister(false, true, false, false, false, false));
            nrf.WriteRegister(new ReceiverAddressRegister(true, true, false, false, false, false));
            nrf.WriteRegister(new AddressWidthRegister(AddressWidthEnum.FiveBytes));
            nrf.WriteRegister(new AutoRetransmissionRegister(AutoRetransmitDelayEnum.T500us, 3));
            nrf.WriteRegister(new FrequencyChannelRegister(66));
            nrf.WriteRegister(new RadioRegister(false, false, DataRateEnum.S1Mbs, OutputPowerEnum.Pminus6dbm));

            nrf.SetPipeReceiveAddress(0, HubAddress);

            nrf.WriteRegister(RegisterEnum.Pipe0PayloadSize, Nrf.PayloadSize);
            nrf.WriteRegister(RegisterEnum.Pipe1PayloadSize, Nrf.PayloadSize);

            //Nrf.ConfigureInterrupt(Configuration.InterruptPin, true, false, false, InterruptHandler);

            var statusAfterFlush = nrf.Flush();

            Console.WriteLine($"Status after Flush(): ({(byte)statusAfterFlush}) {statusAfterFlush}");
            Console.WriteLine($"Frequency: {Decimal.Divide(nrf.Frequency, 1000000U):F3}MHz");
            
            // now, read back all
            var generalRegister = (GeneralRegister)nrf.ReadRegister(RegisterEnum.General);
            var autoAcknowledgmentRegister = (AutoAcknowledgmentRegister)nrf.ReadRegister(RegisterEnum.AutoAcknowledgment);
            var receiverAddressRegister = (ReceiverAddressRegister)nrf.ReadRegister(RegisterEnum.ReceiverAddress);
            var addressWidthRegister = (AddressWidthRegister)nrf.ReadRegister(RegisterEnum.AddressWidth);
            var autoRetransmissionRegister = (AutoRetransmissionRegister)nrf.ReadRegister(RegisterEnum.AutoRetransmission);
            var frequencyChannelRegister = (FrequencyChannelRegister)nrf.ReadRegister(RegisterEnum.FrequencyChannel);
            var radioRegister = (RadioRegister)nrf.ReadRegister(RegisterEnum.Radio);

            var pipe0Address = nrf.ReadPipeAddress(0);

            Console.WriteLine();
            Console.WriteLine($"GeneralRegister: ({(byte)generalRegister:X2}) {generalRegister}");
            Console.WriteLine($"AutoAcknowledgmentRegister: ({(byte)autoAcknowledgmentRegister:X2}) {autoAcknowledgmentRegister}");
            Console.WriteLine($"ReceiverAddressRegister: ({(byte)receiverAddressRegister:X2}) {receiverAddressRegister}");
            Console.WriteLine($"AddressWidthRegister: ({(byte)addressWidthRegister:X2}) {addressWidthRegister}");
            Console.WriteLine($"AutoRetransmissionRegister: ({(byte)autoRetransmissionRegister:X2}) {autoRetransmissionRegister}");
            Console.WriteLine($"FrequencyChannelRegister: ({(byte)frequencyChannelRegister:X2}) {frequencyChannelRegister}");
            Console.WriteLine($"RadioRegister: ({(byte)radioRegister:X2}) {radioRegister}");
            Console.WriteLine($"Pipe0Address: {pipe0Address}");

            return -1;

            // PowerDown

            nrf.SetupDelay();
            nrf.ModifyRegister(RegisterEnum.General, GeneralRegister.PowerUp);

            // Standby I - after power up

            //Nrf.StartListening();

            // RX mode - after Receiver mode and CE high

            Console.WriteLine("Sending...");

            nrf.SetTransmitAddress(new Address("kizi1"), true);
            var payload = new Packet(HubAddress, "testing 123...");
            var result = nrf.WritePayload(payload, true);

            Console.WriteLine("Result: " + result);

            //Console.WriteLine("Main loop starting...");
            //while (_loop) { }

            //Nrf.StopListening();

            nrf.ModifyRegister(RegisterEnum.General, GeneralRegister.PowerDown);
            
            return 0;
        }

        private static void SignalHandler(SignalTypeEnum signal)
        {
            Console.WriteLine("Caught signal " + signal + ". Stopping...");

            _loop = false;
        }
    }
}
*/