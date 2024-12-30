using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xunit.Abstractions;

namespace GrpcExtension.Rx.Test.Mocks
{
    public class MockSubject<T>
    {
        public MockSubject(int number, ITestOutputHelper? testOutputHelper = null)
        {
            Subject = new Subject<T>();
            Id = Guid.NewGuid();
            Number = number;
            Subject
            .Catch<T, Exception>(ex =>
            {
                testOutputHelper?.WriteLine($"Caught exception: {ex.Message}");
                return Observable.Empty<T>();
            }).Subscribe(onCompleted: () => 
            {
                IsCompleted = true;
            },
            onNext: t => 
            {
                OnNextCount++;
            });
        }
        public Subject<T> Subject { get; private set; }
        public Guid Id { get; private set; }
        public int Number { get; private set; }
        public bool IsCompleted { get; private set; }
        public int OnNextCount { get; private set; }
    }
}
