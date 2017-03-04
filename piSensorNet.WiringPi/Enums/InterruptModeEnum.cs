using System;
using System.Linq;

namespace piSensorNet.WiringPi.Enums
{
    /*
        #define	INT_EDGE_SETUP		0
        #define	INT_EDGE_FALLING	1
        #define	INT_EDGE_RISING		2
        #define	INT_EDGE_BOTH		3
        #define	INT_EDGE_NONE		4
    */
    public enum InterruptModeEnum
    {
        AlreadySetup = 0,
        FallingEdge = 1,
        RisingEdge = 2,
        BothEdges = 3,
        None = 4
    }
}