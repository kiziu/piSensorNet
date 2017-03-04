using System;
using System.Linq;

namespace piSensorNet.WiringPi.Enums
{
    /*
        #define	INPUT			 0
        #define	OUTPUT			 1
        #define	PWM_OUTPUT		 2
        #define	GPIO_CLOCK		 3
        #define	SOFT_PWM_OUTPUT		 4
        #define	SOFT_TONE_OUTPUT	 5
        #define	PWM_TONE_OUTPUT		 6
    */
    public enum PinModeEnum
    {
        Input = 0,
        Output = 1
    }
}