using System;
using System.Linq;
using System.Threading;

namespace piSensorNet.WiringPi
{
    public sealed class ThreadStopper : IDisposable
    {
        private bool _disposed;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public ThreadStopper(Thread thread)
        {
            _thread = thread;

            _thread.Start(_tokenSource.Token);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _tokenSource.Cancel();

            _thread.Join();
        }
    }
}