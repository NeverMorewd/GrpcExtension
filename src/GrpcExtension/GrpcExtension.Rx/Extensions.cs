using Grpc.Core;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
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
        public static IObservable<T> MakeReplayObservale<T>(this IAsyncStreamReader<T> asyncStreamReader, int buffer)
        {
            var subject = new ReplaySubject<T>(buffer);
            return MakeObservableReaderCore(asyncStreamReader, subject);
        }
        public static IObservable<T> MakeObservale<T>(this IAsyncStreamReader<T> asyncStreamReader)
        {
            var subject = new Subject<T>();
            return MakeObservableReaderCore(asyncStreamReader, subject);
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
                ex => tcs.SetResult(false),
                () => tcs.SetResult(true));

            return tcs.Task;
        }
        public static IDisposable WriteTo<T>(this IAsyncStreamWriter<T> asyncStreamWriter, IObservable<T> observable)
        {
            var channel = Channel.CreateUnbounded<T>();
            _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await foreach (var item in channel.Reader.ReadAllAsync())
                    {
                        await asyncStreamWriter.WriteAsync(item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }, creationOptions: TaskCreationOptions.LongRunning);

            return observable.Subscribe(
                onNext: item =>
                { 
                    channel.Writer.TryWrite(item); 
                },
                onError: error =>
                {
                    channel.Writer.Complete(error);
                },
                onCompleted: () =>
                {
                    channel.Writer.Complete();
                });
        }
        public static IDisposable WriteTo<T>(this IAsyncStreamWriter<T> asyncStreamWriter, IObservable<T> observable, IObservable<T> onErrorResumeObservable)
        {
            var channel = Channel.CreateUnbounded<T>();
            _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await foreach (var item in channel.Reader.ReadAllAsync())
                    {
                        await asyncStreamWriter.WriteAsync(item);
                    }
                }
                catch (Exception ex)
                {

                }
            }, creationOptions: TaskCreationOptions.LongRunning);

            return observable
                .OnErrorResumeNext(onErrorResumeObservable)
                .Subscribe(
                onNext: item =>
                {
                    channel.Writer.TryWrite(item);
                },
                onError: error =>
                {
                    channel.Writer.Complete(error);
                },
                onCompleted: () =>
                {
                    channel.Writer.Complete();
                });
        }


        private static IObservable<T> MakeObservableReaderCore<T>(IAsyncStreamReader<T> asyncStreamReader, ISubject<T> subject)
        {
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
    }
}
