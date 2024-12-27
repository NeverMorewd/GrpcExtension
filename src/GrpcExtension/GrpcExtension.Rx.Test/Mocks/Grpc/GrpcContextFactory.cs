using Grpc.Core;
using GrpcExtension.Rx.Test.GrpcServices;
using Xunit.Abstractions;

namespace GrpcExtension.Rx.Test.Mocks.Grpc
{
    public class GrpcContextFactory
    {
        private static readonly ChannelOption[] Options =
        {
            new(ChannelOptions.MaxReceiveMessageLength, 1024 * 1024 * 1024),
            new(ChannelOptions.MaxSendMessageLength, 1024 * 1024 * 1024)
        };
        private int _port;

        public GrpcChannelContext Create(ITestOutputHelper output = null)
        {
            var impl = new TestServiceImpl();
            var server = new Server(Options)
            {
                Services = { TestService.BindService(impl) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();
            _port = server.Ports.First().BoundPort;
            return new GrpcChannelContext
            {
                Service = impl,
                Client = CreateClient(output),
                OnDispose = () => server.KillAsync()
            };
        }

        public TestService.TestServiceClient CreateClient(ITestOutputHelper output = null)
        {
            var channel = new Channel(
                "localhost",
                _port,
                ChannelCredentials.Insecure,
                Options);
            return new TestService.TestServiceClient(channel);
        }

        public override string ToString()
        {
            return "http";
        }
    }
}
