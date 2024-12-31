using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GrpcExtension.Rx
{
    public class ReactiveAsyncStreamWriter<T> : IDisposable
    {
        private readonly Channel<T> _channel;
        private readonly Task _writingTask;
        private readonly IClientStreamWriter<T> _asyncStreamWriter;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _cancellationToken;
        private IObservable<T> _source;
        private bool _isDisposed;

        public ReactiveAsyncStreamWriter(IClientStreamWriter<T> asyncStreamWriter)
        {
            _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
            });
            _asyncStreamWriter = asyncStreamWriter ?? throw new ArgumentNullException(nameof(asyncStreamWriter));
            _source = new Subject<T>();
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;

            _writingTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await foreach (var item in _channel.Reader.ReadAllAsync(_cancellationToken))
                    {
                        if (_cancellationToken.IsCancellationRequested)
                            break;

                        await _asyncStreamWriter.WriteAsync(item);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    // Consider using a proper logging framework
                }
            }, _cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public IClientStreamWriter<T> AsyncStreamWriter => _asyncStreamWriter;

        public ReactiveAsyncStreamWriter<T> With(IObservable<T> observable)
        {
            if (observable == null)
                throw new ArgumentNullException(nameof(observable));

            _source = _source.Merge(observable.OnErrorResumeNext(Observable.Empty<T>()));
            return this;
        }
        public ReactiveAsyncStreamWriter<T> With(IEnumerable<IObservable<T>> observables)
        {
            if (observables == null)
                throw new ArgumentNullException(nameof(observables));

            foreach (var observable in observables)
            {
                With(observable);
            }
            return this;
        }

        public IDisposable Writing()
        {
            ThrowIfDisposed();

            return _source.Subscribe(
                onNext: item =>
                {
                    if (!_channel.Writer.TryWrite(item))
                    {
                        throw new InvalidOperationException("Failed to write to channel");
                    }
                },
                onError: error =>
                {
                    //_channel.Writer.Complete(error);
                },
                onCompleted: () =>
                {
                    //_channel.Writer.Complete();
                    //await _asyncStreamWriter.CompleteAsync();
                    Console.WriteLine($"ReactiveAsyncStreamWriter onCompleted");
                });
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ReactiveAsyncStreamWriter<T>));
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                //_asyncStreamWriter.CompleteAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                _cts.Cancel();
                _channel.Writer.Complete();
                _writingTask.Wait(TimeSpan.FromSeconds(5));
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }
}