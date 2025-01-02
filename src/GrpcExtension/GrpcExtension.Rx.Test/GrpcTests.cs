using GrpcExtension.Rx.Test.GrpcServices;
using GrpcExtension.Rx.Test.Mocks;
using GrpcExtension.Rx.Test.Mocks.Grpc;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GrpcExtension.Rx.Test
{
    public class GrpcTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly GrpcContextFactory _grpcContextFactory;
        public GrpcTests(ITestOutputHelper testOutputHelper)
        {
            _grpcContextFactory = new GrpcContextFactory();
            _outputHelper = testOutputHelper;
        }
        [Fact]
        public async Task SimpleUnaryAsync()
        {
            using var channelContext = _grpcContextFactory.Create(_outputHelper);
            var response = await channelContext.Client.SimpleUnaryAsync(new RequestMessage { Value = 10 });
            Assert.Equal(10, response.Value);
            Assert.True(channelContext.Service.SimplyUnaryCalled);
        }

        [Fact]
        public async Task ClientStreamingAsync()
        {
            // Arrange
            const int messageCount = 10;
            const int subjectCount = 100;
            const int sendValue = 10;
            const int delayMs = 100;
            

            using var channelContext = _grpcContextFactory.Create(_outputHelper);
            var subjects = new List<MockSubject<RequestMessage>>();
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(messageCount * delayMs * 2);

            var call = channelContext.Client.ClientStreaming();

            try
            {
                subjects.AddRange(Enumerable.Range(1, subjectCount)
                    .Select(no => new MockSubject<RequestMessage>(no, _outputHelper)));

                using var rxWriter = new ReactiveAsyncStreamWriter<RequestMessage>(call.RequestStream);
                rxWriter.With(subjects.Select(s => s.Subject));

                using var subscription = rxWriter.Writing();
                var tasks = subjects.Select(subject => Task.Run(async () =>
                {
                    for (int i = 0; i < messageCount && !cts.Token.IsCancellationRequested; i++)
                    {
                        subject.Subject.OnNext(new RequestMessage { Value = sendValue });
                        try
                        {
                            await Task.Delay(delayMs, cts.Token);
                        }
                        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    if (!subject.IsCompleted)
                    {
                        subject.Subject.OnError(new Exception($"{subject.Number}:dummy error"));
                        _outputHelper.WriteLine($"{subject.Number} oncompleted!");
                        subject.Subject.OnCompleted();
                    }
                }, cts.Token));

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    _outputHelper.WriteLine("Writing tasks were cancelled");
                    throw;
                }
                await call.RequestStream.CompleteAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                var responseTask = call.ResponseAsync;

                var completedTask = await Task.WhenAny(responseTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Response not received within timeout period");
                }

                var response = await responseTask;

                int expectedTotal = messageCount * subjectCount * sendValue;
                _outputHelper.WriteLine($"Response: {response.Value}");
                Assert.Equal(expectedTotal, response.Value);

                foreach (var subject in subjects)
                {
                    Assert.True(subject.IsCompleted, $"Subject {subject.Number} did not complete");
                    Assert.Equal(messageCount, subject.OnNextCount);
                }
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine($"Test failed with exception: {ex}");
                throw;
            }
        }
        [Fact]
        public async Task ServerStreamingAsync()
        {
            const int observerCount = 100;
            const int requestValue = 200;
            var items = new List<int>();
            var completeds = new List<bool>();
            using var channelContext = _grpcContextFactory.Create(_outputHelper);
            using var call = channelContext.Client.ServerStreaming(new RequestMessage { Value = requestValue });
            var streamingReaderObservable = call.ResponseStream.MakeObservale();

            var factory = new ObserverFactory(observerCount)
            {
                TestOutput = _outputHelper
            };
            foreach (var observer in factory.Observers)
            {
                streamingReaderObservable
                    .Subscribe(onNext: i =>
                    {
                        observer.OnNext(i.Value);
                        items.Add(i.Value);
                    },
                    onError: e =>
                    {
                        observer.OnError(e);
                    },
                    onCompleted: () =>
                    {
                        observer.OnCompleted();
                        completeds.Add(true);
                    });
            }

            await streamingReaderObservable.WaitForCompletionAsync();
            var groups = items.GroupBy(i => i).ToList();
            Assert.Equal(requestValue, groups.Count);
            foreach (var group in groups)
            {
                Assert.Equal(observerCount, group.Count());
                Assert.True(group.All(i=>i == group.Key));
            }
            Assert.True(completeds.All(c => c));
        }
    }
}
