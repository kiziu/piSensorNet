using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public void Enqueue(IDisposable item)
        {
            _queue.Enqueue(item);
        }

        public int Count => _queue.Count;

        public IEnumerator<IDisposable> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
