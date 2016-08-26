using System;
using System.Linq;

using _Console = System.Console;

namespace piSensorNet.Common.Helpers
{
    public static class LoggingHelper
    {
        public static Action<string> ToConsole { get; } =
            i => _Console.WriteLine($"{DateTime.Now.ToString("O")}: {i}");
    }
}
