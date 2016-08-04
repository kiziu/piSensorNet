using System;
using System.Linq;

namespace piSensorNet.Common.System
{
    public enum SignalTypeEnum
    {
        HangUp = 1,
        Interrupt = 2,
        Quit = 3,
        IllegalInstruction = 4,
        TraceTrap = 5,
        Abort = 6,
        BusError = 7,
        FloatingPointException = 8,
        Kill = 9,
        User1 = 10,
        SegmentationViolation = 11,
        User2 = 12,
        BrokenPipe = 13,
        AlarmClock = 14,
        Terminate = 15,
        StackFault = 16,
        ChildStatusChange = 17,
        Continue = 18,
        Stop = 19,
        KeyboardStop = 20,
        BackgroundReadFromTTY = 21,
        BackgroundWriteFromTTY = 22,
        UrgentConditionToSocket = 23,
        CPULimitExceeded = 24,
        FileSizeLimitExceeded = 25,
        VirtualAlarmClock = 26,
        ProfilingAlarmClock = 27,
        WindowSizeChange = 28,
        IOPossible = 29,
        PowerFailureRestart = 30,
        BadSystemCall = 31
    }
}