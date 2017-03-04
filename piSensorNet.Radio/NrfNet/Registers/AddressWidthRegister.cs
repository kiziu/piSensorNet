using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class AddressWidthRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.AddressWidth;
        public byte Value => this;

        public AddressWidthEnum Width { get; set; }

        private AddressWidthRegister() {}

        public AddressWidthRegister(AddressWidthEnum width)
        {
            Width = width;
        }

        public override string ToString()
        {
            return $"[Width: {Width}]";
        }

        public static implicit operator byte(AddressWidthRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBits(ref output, 0, 2, (byte)input.Width);

            return output;
        }

        public static implicit operator AddressWidthRegister(byte input)
        {
            var output = new AddressWidthRegister
                         {
                             Width = (AddressWidthEnum)input.GetBits(0, 2),
                         };

            return output;
        }
    }
}