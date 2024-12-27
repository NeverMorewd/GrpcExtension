using System.Reactive.Subjects;

namespace GrpcExtension.Rx.Test.Mocks
{
    public class MockSubject<T>
    {
        public MockSubject(int number)
        {
            Subject = new Subject<T>();
            Id = Guid.NewGuid();
            Number = number;
        }
        public Subject<T> Subject { get; private set; }
        public Guid Id { get; private set; }
        public int Number { get; private set; }
    }
}
