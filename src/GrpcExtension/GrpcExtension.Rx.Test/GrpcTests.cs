using GrpcExtension.Rx.Test.GrpcServices;
using GrpcExtension.Rx.Test.Mocks;
using GrpcExtension.Rx.Test.Mocks.Grpc;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

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
            const int subjectCount = 20;
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

                int expectedTotal = messageCount * subjectCount * 10; // 每个消息值为10
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
            finally
            {
                foreach (var subject in subjects)
                {
                    try
                    {
                        if (!subject.IsCompleted)
                        {
                            subject.Subject.OnCompleted();
                        }
                        subject.Subject.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _outputHelper.WriteLine($"Error during subject cleanup: {ex}");
                    }
                }
                cts.Dispose();
            }
        }
        [Fact]
        public async Task ClientStreamingAsyncTest()
        {
            using var channelContext = _grpcContextFactory.Create(_outputHelper);
            var call = channelContext.Client.ClientStreaming();


            var tasks = Enumerable.Range(1, 10).Select(i => 
            {
                return call.RequestStream.WriteAsync(new RequestMessage 
                {
                    Value = i,
                });
            });

            // Act
            foreach (var item in tasks)
            {
                await item;
            }
            await call.RequestStream.CompleteAsync();
            var response = await call;
            Assert.Equal(55, response.Value);
        }
        [Fact]
        public async Task ServerStreamingAsync()
        {
            using var channelContext = _grpcContextFactory.Create(_outputHelper);
            var response = await channelContext.Client.SimpleUnaryAsync(new RequestMessage { Value = 10 });
            Assert.Equal(10, response.Value);
            Assert.True(channelContext.Service.SimplyUnaryCalled);
        }
    }
}
