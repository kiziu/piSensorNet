using System;
using System.Linq;

namespace piSensorNet.WiringPi.Enums
{
	public enum SpiChannelEnum
	{
		Zero = 0,
		One = 1
	}

	public static class EnumExtensions
    {
        public static PinNumberEnum ToPinNumber(this SpiChannelEnum channel) => channel == SpiChannelEnum.One ? PinNumberEnum.SPI_CE1 : PinNumberEnum.SPI_CE0;
    }
}