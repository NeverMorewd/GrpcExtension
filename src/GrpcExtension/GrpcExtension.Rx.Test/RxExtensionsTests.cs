using Grpc.Core;
using GrpcExtension.Rx;
using GrpcExtension.Rx.Test.Mocks;
public class RxExtensionsTests
{
    

    [Fact]
    public async Task Should_Emit_All_Items()
    {
        // Arrange
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

        // Act
        var observable = reader.MakeObservale();
        var subscription = observable.Subscribe(observer);
        await Task.Delay(100);

        // Assert
        Assert.Equal(expected, actual);
        Assert.True(completed);
        Assert.Null(error);
    }

    

    [Fact]
    public async Task Should_Handle_Empty_Stream()
    {
        // Arrange
        var reader = new MockAsyncStreamReader<int>(Array.Empty<int>());
        var items = new List<int>();
        var completed = false;
        var observer = new TestObserver<int>(
            onNext: value => items.Add(value),
            onCompleted: () => completed = true
        );

        // Act
        var observable = reader.MakeObservale();
        var subscription = observable.Subscribe(observer);
        await Task.Delay(100);

        // Assert
        Assert.Empty(items);
        Assert.True(completed);
    }

    [Fact]
    public async Task Should_Support_Multiple_Observers()
    {
        // Arrange
        var expected = new[] { 1, 2, 3, 4, 5 };
        var reader = new MockAsyncStreamReader<int>(expected);

        var items1 = new List<int>();
        var items2 = new List<int>();
        var completed1 = false;
        var completed2 = false;

        var observer1 = new TestObserver<int>(
            onNext: value => items1.Add(value),
            onCompleted: () => completed1 = true
        );

        var observer2 = new TestObserver<int>(
            onNext: value => items2.Add(value),
            onCompleted: () => completed2 = true
        );

        // Act
        var observable = reader.MakeObservale();
        var subscription1 = observable.Subscribe(observer1);
        var subscription2 = observable.Subscribe(observer2);

        await Task.Delay(100);

        // Assert
        Assert.Equal(expected, items1);
        Assert.Equal(expected, items2);

        Assert.True(completed1);
        Assert.True(completed2);

        Assert.Equal(items1, items2);
    }

    [Fact]
    public async Task Should_Handle_Errors()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var reader = new MockAsyncStreamReader<int>([1], expectedException);
        var items = new List<int>();
        Exception? error = null;
        var observer = new TestObserver<int>(
            onNext: value => items.Add(value),
            onError: ex => error = ex
        );

        // Act
        var observable = reader.MakeObservale();
        var subscription = observable.Subscribe(observer);
        await Task.Delay(100);

        // Assert
        Assert.Single(items);
        Assert.NotNull(error);
        Assert.Equal(expectedException, error);
    }
}