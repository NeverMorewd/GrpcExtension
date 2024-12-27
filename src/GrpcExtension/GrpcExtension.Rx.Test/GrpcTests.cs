using GrpcExtension.Rx.Test.GrpcServices;
using GrpcExtension.Rx.Test.Mocks;
using GrpcExtension.Rx.Test.Mocks.Grpc;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
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
            using var channelContext = _grpcContextFactory.Create(_outputHelper);
            var call = channelContext.Client.ClientStreaming();
            var subjects = Enumerable.Range(1, 10).Select(no => new MockSubject<RequestMessage>(no));

            var tasks
                =
            subjects
            .Select(mockSub =>
            {
                var disposable = call.RequestStream.WriteTo(mockSub.Subject);
                return (mockSub, disposable);
            })
            .Select((tuple) =>
            {
                return Task.Run(async () =>
                {
                    tuple.mockSub.Subject.OnNext(new RequestMessage { Value = 10 });
                    await Task.Delay(100);
                    tuple.mockSub.Subject.OnNext(new RequestMessage { Value = 10 });
                    await Task.Delay(100);
                    tuple.mockSub.Subject.OnNext(new RequestMessage { Value = 10 });
                    await Task.Delay(100);
                    tuple.mockSub.Subject.OnNext(new RequestMessage { Value = 10 });
                    await Task.Delay(100);
                    tuple.mockSub.Subject.OnCompleted();
                    tuple.disposable.Dispose();
                });
            }).ToList();

            // Act
            await Task.WhenAll(tasks);
            var response = await call;
            Assert.Equal(10, response.Value);
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
