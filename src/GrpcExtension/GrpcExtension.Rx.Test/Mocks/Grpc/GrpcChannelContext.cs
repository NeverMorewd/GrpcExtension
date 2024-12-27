using GrpcExtension.Rx.Test.GrpcServices;

namespace GrpcExtension.Rx.Test.Mocks.Grpc
{
    public class GrpcChannelContext : IDisposable
    {
        public Action? OnDispose
        {
            get;
            set;
        }

        public TestService.TestServiceClient Client
        {
            get;
            set;
        }

        public TestServiceImpl Service
        {
            get;
            set;
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}
