using Grpc.Core;
using GrpcExtension.Rx.Internal;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcExtension.Rx
{
    public static class Extensions
    {
        public static void ToObservable<TRequest, TReponse>(this AsyncDuplexStreamingCall<TRequest, TReponse> asyncDuplexStreamingCall)
        {
            
        }
        public static void ToObservable<TRequest, TReponse>(this AsyncClientStreamingCall<TRequest, TReponse> asyncClientStreamingCall)
        {
            
        }
        public static void ToObservable<TReponse>(this AsyncServerStreamingCall<TReponse> asyncServerStreamingCall)
        {

        }
        public static void ToObservale<T>(this IClientStreamWriter<T> clientStreamWriter)
        {

        }
        public static IObservable<T> MakeSimpleObservale<T>(this IAsyncStreamReader<T> asyncStreamReader)
        {
            return new SimpleObservable<T>(async (observer) =>
            {
                try
                {
                    while (await asyncStreamReader.MoveNext(CancellationToken.None))
                    {
                        observer.OnNext(asyncStreamReader.Current);
                    }
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            });
        }
        public static IObservable<T> MakeObservale<T>(this IAsyncStreamReader<T> asyncStreamReader)
        {
            var subject = new ReplaySubject<T>();
            var tcs = new TaskCompletionSource<bool>();
            var startedReading = 0;

            return Observable.Create<T>(observer =>
            {
                if (Interlocked.CompareExchange(ref startedReading, 1, 0) == 0)
                {
                    async Task ReadStream()
                    {
                        try
                        {
                            while (await asyncStreamReader.MoveNext(CancellationToken.None))
                            {
                                subject.OnNext(asyncStreamReader.Current);
                            }
                            subject.OnCompleted();
                            tcs.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            subject.OnError(ex);
                            tcs.SetException(ex);
                        }
                    }

                    _ = ReadStream();
                }

                return new CompositeDisposable(
                    subject.Subscribe(observer),
                    Disposable.Create(() => { })
                );
            });
        }

        public static Task WaitForCompletionAsync<T>(this IObservable<T> observable)
        {
            var tcs = new TaskCompletionSource<bool>();
            var subscription = observable.Subscribe(
                _ => { },
                ex => tcs.SetException(ex),
                () => tcs.SetResult(true));

            return tcs.Task;
        }
        public static Task WaitForCompletionIgnoreErrorAsync<T>(this IObservable<T> observable)
        {
            var tcs = new TaskCompletionSource<bool>();
            var subscription = observable.Subscribe(
                _ => { },
                ex => tcs.SetResult(true),
                () => tcs.SetResult(true));

            return tcs.Task;
        }
        public static void ToObservale<T>(this IAsyncStreamWriter<T> asyncStreamWriter)
        {

        }
    }
}
