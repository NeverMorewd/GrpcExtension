using GrpcExtension.Rx.Test.Mocks;
using System.Reactive.Linq;
using Xunit.Abstractions;

namespace GrpcExtension.Rx.Test
{
    public class StreamReaderExtensionsTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public StreamReaderExtensionsTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_Emit_All_Items()
        {
            var expected = new[] { 1, 2, 3, 4, 5 };
            var reader = new MockAsyncStreamReader<int>(expected);
            var actual = new List<int>();
            var completed = false;
            Exception? error = null;
            var observer = new TestObserver<int>(
                onNext: value => actual.Add(value),
                onError: ex => error = ex,
                onCompleted: () => completed = true
            );

            var observable = reader.MakeObservale();
            var subscription = observable.Subscribe(observer);
            await Task.Delay(100);

            Assert.Equal(expected, actual);
            Assert.True(completed);
            Assert.Null(error);
        }



        [Fact]
        public async Task Should_Handle_Empty_Stream()
        {
            var reader = new MockAsyncStreamReader<int>(Array.Empty<int>());
            var items = new List<int>();
            var completed = false;
            var observer = new TestObserver<int>(
                onNext: value => items.Add(value),
                onCompleted: () => completed = true
            );

            var observable = reader.MakeObservale();
            var subscription = observable.Subscribe(observer);
            await observable.WaitForCompletionAsync();

            Assert.Empty(items);
            Assert.True(completed);
        }
        [Fact]
        public async Task Performance_Multiple_Observers()
        {
            var expected = Enumerable.Range(0, 99);
            var reader = new MockAsyncStreamReader<int>(expected);
            var items = new List<int>();
            var completeds = new List<bool>();
            var factory = new ObserverFactory
            {
                TestOutput = _testOutputHelper
            };
            var observable = reader.MakeObservale();
            foreach (var observer in factory.Observers)
            {
                observable
                    .Subscribe(onNext: i =>
                    {
                        observer.OnNext(i);
                        items.Add(i);
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

            await observable.WaitForCompletionAsync();

            Assert.Equal(factory.Observers.Count() * expected.Count(), items.Count);
            Assert.Equal(completeds.Count, factory.Observers.Count());
            Assert.True(completeds.All(c => c));
        }

        [Fact]
        public async Task Should_Handle_Errors()
        {
            var expectedException = new InvalidOperationException("Test error");
            var target = Enumerable.Range(0, 99);
            var reader = new MockAsyncStreamReader<int>(target, expectedException);
            var items = new List<int>();
            Exception? error = null;
            var observer = new TestObserver<int>(
                onNext: value => items.Add(value),
                onError: ex => error = ex
            );

            var observable = reader.MakeObservale();
            var subscription = observable.Subscribe(observer);
            await observable.WaitForCompletionIgnoreErrorAsync();

            Assert.Equal(items.Count, target.Count());
            Assert.NotNull(error);
            Assert.Equal(expectedException, error);
        }

        [Theory]
        [ClassData(typeof(ObserverFactory))]
        public async Task Multiple_Observers(IObserver<int> observer)
        {
            var expected = Enumerable.Range(0, 99);
            var reader = new MockAsyncStreamReader<int>(expected);

            var items = new List<int>();
            var completed = false;

            var observable = reader.MakeObservale();
            var subscription1 = observable
                .Subscribe(onNext: i =>
                {
                    observer.OnNext(i);
                    items.Add(i);
                },
                onError: e =>
                {
                    observer.OnError(e);
                },
                onCompleted: () =>
                {
                    observer.OnCompleted();
                    completed = true;
                });

            await observable.WaitForCompletionAsync();
            Assert.Equal(expected, items);
            Assert.True(completed);
        }
    }
}