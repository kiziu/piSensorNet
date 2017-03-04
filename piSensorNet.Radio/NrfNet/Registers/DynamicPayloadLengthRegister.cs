using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class DynamicPayloadLengthRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.DynamicPayloadLength;
        public byte Value => this;

        public bool EnableOnPipe0 { get; set; }
        public bool EnableOnPipe1 { get; set; }
        public bool EnableOnPipe2 { get; set; }
        public bool EnableOnPipe3 { get; set; }
        public bool EnableOnPipe4 { get; set; }
        public bool EnableOnPipe5 { get; set; }

        private DynamicPayloadLengthRegister() {}

        public DynamicPayloadLengthRegister(bool enableOnPipe0, bool enableOnPipe1, bool enableOnPipe2, bool enableOnPipe3, bool enableOnPipe4, bool enableOnPipe5)
        {
            EnableOnPipe0 = enableOnPipe0;
            EnableOnPipe1 = enableOnPipe1;
            EnableOnPipe2 = enableOnPipe2;
            EnableOnPipe3 = enableOnPipe3;
            EnableOnPipe4 = enableOnPipe4;
            EnableOnPipe5 = enableOnPipe5;
        }

        public override string ToString()
        {
            return $"[EnableOnPipe0: {EnableOnPipe0}, EnableOnPipe1: {EnableOnPipe1}, EnableOnPipe2: {EnableOnPipe2}, EnableOnPipe3: {EnableOnPipe3}, EnableOnPipe4: {EnableOnPipe4}, EnableOnPipe5: {EnableOnPipe5}]";
        }

        public static implicit operator byte(DynamicPayloadLengthRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBit(ref output, 0, input.EnableOnPipe0);
            ByteExtensions.SetBit(ref output, 1, input.EnableOnPipe1);
            ByteExtensions.SetBit(ref output, 2, input.EnableOnPipe2);
            ByteExtensions.SetBit(ref output, 3, input.EnableOnPipe3);
            ByteExtensions.SetBit(ref output, 4, input.EnableOnPipe4);
            ByteExtensions.SetBit(ref output, 5, input.EnableOnPipe5);

            return output;
        }

        public static implicit operator DynamicPayloadLengthRegister(byte input)
        {
            var output = new DynamicPayloadLengthRegister
                         {
                             EnableOnPipe0 = input.GetBit(0),
                             EnableOnPipe1 = input.GetBit(1),
                             EnableOnPipe2 = input.GetBit(2),
                             EnableOnPipe3 = input.GetBit(3),
                             EnableOnPipe4 = input.GetBit(4),
                             EnableOnPipe5 = input.GetBit(5),
                         };

            return output;
        }
    }
}