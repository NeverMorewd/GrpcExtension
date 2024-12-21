using System;
using System.Threading.Tasks;

namespace GrpcExtension.Rx.Internal
{
    public class SimpleObservable<T> : IObservable<T>
    {
        private readonly Func<IObserver<T>, Task> _subscribe;

        public SimpleObservable(Func<IObserver<T>, Task> subscribe)
        {
            _subscribe = subscribe;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    await _subscribe(observer);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            });

            return new SimpleSubscription<T>(() => { });
        }
    }

}
