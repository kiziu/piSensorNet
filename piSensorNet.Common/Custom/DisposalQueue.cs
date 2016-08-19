using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Custom
{
    public sealed class DisposalQueue : IReadOnlyCollection<IDisposable>, IDisposable
    {
        private readonly Queue<IDisposable> _queue = new Queue<IDisposable>();
        
        public void Dispose()
        {
            while (_queue.Count > 0)
                _queue.Dequeue().Dispose();
        }

        [NotNull]
        public DisposalQueue Enqueue([NotNull] IDisposable item)
        {
            _queue.Enqueue(item);

            return this;
        }

        public int Count => _queue.Count;

        public IEnumerator<IDisposable> GetEnumerator() 
            => _queue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

        [NotNull]
        public static DisposalQueue operator +([NotNull] DisposalQueue queue, [NotNull] IDisposable item) 
            => queue.Enqueue(item);
    }
}
