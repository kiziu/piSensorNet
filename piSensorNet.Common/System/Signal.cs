using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;

namespace piSensorNet.Common.System
{
    public static class Signal
    {
        private const string LibraryPath = "/usr/lib/gcc/arm-linux-gnueabihf/4.9.2/libstdc++.so";

        private delegate void SignalHandler(int signalNumber);

        [DllImport(LibraryPath, EntryPoint = "signal", SetLastError = true)]
        private static extern IntPtr bindSignalHandler(int signal, SignalHandler handler);

        [DllImport(LibraryPath, EntryPoint = "kill", SetLastError = true)]
        private static extern int sendSignal(int pid, int sign);

        [DllImport(LibraryPath, EntryPoint = "raise", SetLastError = true)]
        private static extern int raiseSignal(int sign);

        [Obsolete("Does't work well...")]
        public static IReadOnlyList<IntPtr> BindHandler(Action<SignalTypeEnum> handler, params SignalTypeEnum[] signals)
        {
            var signalHandler = new SignalHandler(i => handler((SignalTypeEnum)i));

            var result = new List<IntPtr>(signals.Length);
            foreach (var signal in signals)
                result.Add(bindSignalHandler((int)signal, signalHandler));

            return result;
        }

        public static bool Send(int processID, SignalTypeEnum signal)
        {
            var result = sendSignal(processID, (int)signal);

            return result == 0;
        }

        public static bool Raise(SignalTypeEnum signal)
        {
            var result = raiseSignal((int)signal);

            return result == 0;
        }

        public static IDisposable Handle(IReadOnlyDictionary<SignalTypeEnum, Action<SignalTypeEnum>> handlers)
        {
            var thread = new Thread(o =>
            {
                // TODO KZ: windows only
                if (Constants.IsWindows)
                    return;

                var token = (CancellationToken)o;
                var signals = handlers.Keys.Select(i => new UnixSignal((Signum)(int)i)).ToArray();

                while (!token.IsCancellationRequested)
                {
                    var index = UnixSignal.WaitAny(signals, 100);
                    if (index > 50)
                        continue;

                    var signal = signals[index].Signum;
                    var signalType = (SignalTypeEnum)(int)signal;
                    var handler = handlers[signalType];

                    handler(signalType);
                }
            });

            return new ThreadStopper(thread);
        }
    }
}

