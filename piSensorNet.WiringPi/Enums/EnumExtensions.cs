using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace piSensorNet.WiringPi.Enums
{
    public static class EnumExtensions
    {
        public static PinNumberEnum ToPinNumber(this SpiChannelEnum channel) => channel == SpiChannelEnum.One ? PinNumberEnum.SPI_CE1 : PinNumberEnum.SPI_CE0;
    }
}
