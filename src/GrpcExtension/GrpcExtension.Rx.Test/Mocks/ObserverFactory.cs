using System.Collections;
using Xunit.Abstractions;

namespace GrpcExtension.Rx.Test.Mocks
{
    public class ObserverFactory : IEnumerable<object[]>
    {
        private readonly IEnumerable<IObserver<int>> _observers;
        public ObserverFactory() 
        {
            _observers = Enumerable.Range(0, 100).Select(index => 
            {
                return new TestObserver<int>(
                    key: index.ToString(),
                    onNext: i => 
                    { 
                        Console.WriteLine($"{index}:{i}");
                        TestOutput?.WriteLine($"{index}:{i}");
                    },
                    onError: e => 
                    {
                        Console.WriteLine($"{index}:{e}");
                        TestOutput?.WriteLine($"{index}:{e}");
                    },
                    onCompleted: () => 
                    { 
                        Console.WriteLine($"{index}:onCompleted");
                        TestOutput?.WriteLine($"{index}:onCompleted");
                    });
            });
        }
        public ObserverFactory(int count)
        {
            _observers = Enumerable.Range(0, count).Select(index =>
            {
                return new TestObserver<int>(
                    key: index.ToString(),
                    onNext: i =>
                    {
                        Console.WriteLine($"{index}:{i}");
                        TestOutput?.WriteLine($"{index}:{i}");
                    },
                    onError: e =>
                    {
                        Console.WriteLine($"{index}:{e}");
                        TestOutput?.WriteLine($"{index}:{e}");
                    },
                    onCompleted: () =>
                    {
                        Console.WriteLine($"{index}:onCompleted");
                        TestOutput?.WriteLine($"{index}:onCompleted");
                    });
            });
        }
        public IEnumerator<object[]> GetEnumerator()
        {
            return _observers.Select(ob => new object[] { ob }).GetEnumerator();
        }
        public IEnumerable<IObserver<int>> Observers => _observers;
        public ITestOutputHelper? TestOutput
        {
            get;
            set;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
