using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Engine.Master
{
    public sealed class ErrorEventArgs : EventArgs
    {
        [NotNull]
        public Exception Exception { get; }

        public ErrorEventArgs([NotNull] Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            Exception = exception;
        }
    }
}