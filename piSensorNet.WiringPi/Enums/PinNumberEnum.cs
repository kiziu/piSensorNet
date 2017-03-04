using System;
using System.Linq;

namespace piSensorNet.WiringPi.Enums
{
    /// <summary>
    /// http://raspi.tv/wp-content/uploads/2014/07/Raspberry-Pi-GPIO-pinouts.png
    /// </summary>
    public enum BroadcomPinNumberEnum
    {
        Gpio2 = 8,
        I2C_SDA = Gpio2,
        Gpio3 = 9,
        I2C_SCL = Gpio3,
        Gpio4 = 7,
        Gpio17 = 0,
        Gpio27 = 2,
        Gpio22 = 3,
        Gpio10 = 12,
        SPI_MOSI = Gpio10,
        Gpio9 = 13,
        SPI_MISO = Gpio9,
        Gpio11 = 14,
        SPI_CLK = Gpio11,
        Gpio5 = 21,
        Gpio6 = 22,
        Gpio13 = 23,
        Gpio19 = 24,
        Gpio26 = 25,
        Gpio14 = 15,
        UART_TxD = Gpio14,
        Gpio15 = 16,
        UART_RxD = Gpio15,
        Gpio18 = 1,
        Gpio23 = 4,
        Gpio24 = 5,
        Gpio25 = 6,
        Gpio8 = 10,
        SPI_ChipEnable0 = Gpio8,
        Gpio7 = 11,
        SPI_ChipEnable1 = Gpio7,
        Gpio12 = 26,
        Gpio16 = 27,
        Gpio20 = 28,
        Gpio21 = 29
    }

    /// <summary>
    /// http://pi4j.com/images/j8header-2b-large.png
    /// </summary>
    public enum PinNumberEnum
    {
        Gpio0 = 0,
        //Pwm0 = Gpio0,
        Gpio1 = 1,
        Gpio2 = 2,
        Gpio3 = 3,
        Gpio4 = 4,
        Gpio5 = 5,
        Gpio6 = 6,
        Gpio7 = 7,
        Gpio21 = 21,
        Gpio22 = 22,
        Gpio23 = 23,
        //Pwm1 = Gpio23,
        Gpio24 = 24,
        Gpio25 = 25,
        Gpio26 = 26,
        Gpio27 = 27,
        Gpio28 = 28,
        Gpio29 = 29,
        Gpio8 = 8,
        I2C_SDA = Gpio8,
        Gpio9 = 9,
        I2C_SCL = Gpio9,
        Gpio10 = 10,
        SPI_CE0 = Gpio10,
        Gpio11 = 11,
        SPI_CE1 = Gpio11,
        Gpio12 = 12,
        SPI_MOSI = Gpio12,
        Gpio13 = 13,
        SPI_MISO = Gpio13,
        Gpio14 = 14,
        SPI_CLK = Gpio14,
        Gpio15 = 15,
        UART_TxD = Gpio15,
        Gpio16 = 16,
        UART_RxD = Gpio16
    }
}