namespace GrpcExtension.Rx.Test.Mocks
{
    internal class TestObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception?> _onError;
        private readonly Action _onCompleted;
        private readonly string? _key;

        internal TestObserver(string? key = null,
            Action<T>? onNext = null,
            Action<Exception?>? onError = null,
            Action? onCompleted = null)
        {
            _key = key;
            _onNext = onNext ?? (_ => { });
            _onError = onError ?? (_ => { });
            _onCompleted = onCompleted ?? (() => { });
        }

        public void OnNext(T value) => _onNext(value);
        public void OnError(Exception error) => _onError(error);
        public void OnCompleted() => _onCompleted();
        public string Key => _key ?? "null";
    }
}
