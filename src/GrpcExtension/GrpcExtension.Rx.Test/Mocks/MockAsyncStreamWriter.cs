using Grpc.Core;

namespace GrpcExtension.Rx.Test.Mocks
{
    public class MockAsyncStreamWriter<T> : IAsyncStreamWriter<T>
    {
        public List<T> WrittenItems { get; } = [];
        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T item)
        {
            return Task.Run(() =>
            {
                WrittenItems.Add(item);
            });
        }
        public Task WriteAsync(T item, CancellationToken cancellationToken)
        {
            return Task.Run(() => 
            {
                WrittenItems.Add(item);
            }, cancellationToken); 
        }
    }
}
