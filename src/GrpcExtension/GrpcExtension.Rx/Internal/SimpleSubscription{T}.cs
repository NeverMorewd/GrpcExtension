using System;

namespace GrpcExtension.Rx.Internal
{
    internal sealed class SimpleSubscription<T> : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public SimpleSubscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe?.Invoke();
                _disposed = true;
            }
        }
    }
}
