using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Engine.Master
{
    public sealed class PayloadSendingFailedEventArgs : EventArgs
    {
        [NotNull]
        public Message Message { get; }

        public bool IsFirstRetry { get; }

        public DateTime Failed { get; }

        public bool Requeue { get; set; }

        public PayloadSendingFailedEventArgs([NotNull] Message message, bool isFirstRetry, DateTime failed)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Message = message;
            IsFirstRetry = isFirstRetry;
            Failed = failed;
        }
    }
}