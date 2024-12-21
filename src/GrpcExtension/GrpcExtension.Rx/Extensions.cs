using Grpc.Core;
using GrpcExtension.Rx.Internal;
using System;
using System.Threading;

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
        public static IObservable<T> MakeObservale<T>(this IAsyncStreamReader<T> asyncStreamReader)
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
        public static void ToObservale<T>(this IAsyncStreamWriter<T> asyncStreamWriter)
        {

        }
    }
}
