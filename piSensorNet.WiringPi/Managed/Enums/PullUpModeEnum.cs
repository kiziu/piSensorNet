using System;
using System.Linq;

namespace piSensorNet.WiringPi.Managed.Enums
{
    /*
        #define	PUD_OFF			 0
        #define	PUD_DOWN		 1
        #define	PUD_UP			 2
    */
    public enum PullUpModeEnum
    {
        Off = 0,
        Down = 1,
        Up = 2
    }
}