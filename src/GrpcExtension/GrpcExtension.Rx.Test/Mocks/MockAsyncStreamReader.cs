using Grpc.Core;

namespace GrpcExtension.Rx.Test.Mocks
{
    internal class MockAsyncStreamReader<T> : IAsyncStreamReader<T>
    {
        private readonly IEnumerable<T> _sourceItems;
        private IEnumerator<T> _enumerator;
        private readonly Exception? _errorToThrow;

        internal MockAsyncStreamReader(IEnumerable<T> items, Exception? errorToThrow = null)
        {
            _sourceItems = items;
            _enumerator = _sourceItems.GetEnumerator();
            _errorToThrow = errorToThrow;
        }

        public T Current => _enumerator.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);

            if (_enumerator.MoveNext())
            {
                return true;
            }
            if (_errorToThrow != null)
            {
                throw _errorToThrow;
            }
            return false;
        }
    }
}
