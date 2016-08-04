using System;
using System.Linq;
using System_Console = System.Console;

namespace piSensorNet.Common.Console
{
    public static class TimeoutRead
    {
        public static ConsoleKeyInfo? ReadKey(TimeSpan timeout)
        {
            Func<ConsoleKeyInfo> readKey = System_Console.ReadKey;

            var result = readKey.BeginInvoke(null, null);

            result.AsyncWaitHandle.WaitOne(timeout);

            if (!result.IsCompleted)
                return null;

            var keyRead = readKey.EndInvoke(result);

            return keyRead;
        }
    }
}
