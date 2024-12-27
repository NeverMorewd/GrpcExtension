using Grpc.Core;
using GrpcExtension.Rx.Test.Mocks;
using Moq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xunit.Abstractions;

namespace GrpcExtension.Rx.Test
{
    public class StreamWriterExtensionsTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public StreamWriterExtensionsTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        [Fact]
        public void WriteTo_ShouldWriteItems_WhenObservableEmitsItems()
        {
            // Arrange
            var mockStreamWriter = new Mock<IAsyncStreamWriter<int>>();
            var observable = new Subject<int>();
            var testMethod = new Func<IAsyncStreamWriter<int>, IObservable<int>, IDisposable>(
                (streamWriter, obs) => streamWriter.WriteTo(obs)
            );

            // Act
            using (testMethod(mockStreamWriter.Object, observable))
            {
                observable.OnNext(1);
                observable.OnNext(2);
                observable.OnCompleted();
            }

            // Assert
            mockStreamWriter.Verify(writer => writer.WriteAsync(1), Times.Once);
            mockStreamWriter.Verify(writer => writer.WriteAsync(2), Times.Once);
            mockStreamWriter.Verify(writer => writer.WriteAsync(It.IsAny<int>()), Times.Exactly(2));
        }

        [Fact]
        public void WriteTo_ShouldCompleteChannel_WhenObservableCompletes()
        {
            // Arrange
            var mockStreamWriter = new Mock<IAsyncStreamWriter<int>>();
            var observable = new Subject<int>();
            var testMethod = new Func<IAsyncStreamWriter<int>, IObservable<int>, IDisposable>(
                (streamWriter, obs) => streamWriter.WriteTo(obs)
            );

            // Act
            using (testMethod(mockStreamWriter.Object, observable))
            {
                observable.OnNext(1);
                observable.OnCompleted();
            }

            // Assert
            mockStreamWriter.Verify(writer => writer.WriteAsync(1), Times.Once);
            mockStreamWriter.Verify(writer => writer.WriteAsync(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void WriteTo_ShouldCompleteWithError_WhenObservableThrowsErrorAndBreakdownIsTrue()
        {
            // Arrange
            var mockStreamWriter = new Mock<IAsyncStreamWriter<int>>();
            var observable = new Subject<int>();
            var testMethod = new Func<IAsyncStreamWriter<int>, IObservable<int>, IDisposable>(
                (streamWriter, obs) => streamWriter.WriteTo(obs)
            );

            // Act
            using (testMethod(mockStreamWriter.Object, observable))
            {
                observable.OnNext(1);
                observable.OnError(new Exception("Test Error"));
            }

            // Assert
            mockStreamWriter.Verify(writer => writer.WriteAsync(1), Times.Once);
            mockStreamWriter.Verify(writer => writer.WriteAsync(It.IsAny<int>()), Times.Once);
        }
        [Fact]
        public void WriteTo_ShouldResume_WhenObservableThrowsError()
        {
            // Arrange
            var mockStreamWriter = new Mock<IAsyncStreamWriter<int>>();
            var observable = new Subject<int>();
            var resumeObservable = new Subject<int>();
            var testMethod = new Func<IAsyncStreamWriter<int>, IObservable<int>, IDisposable>(
                (streamWriter, obs) => streamWriter.WriteTo(obs, resumeObservable)
            );

            // Act
            using (testMethod(mockStreamWriter.Object, observable))
            {
                observable.OnNext(1);
                observable.OnError(new Exception("Test Error"));
                resumeObservable.OnNext(2);
                resumeObservable.OnCompleted();
            }

            // Assert
            mockStreamWriter.Verify(writer => writer.WriteAsync(1), Times.Once);
            mockStreamWriter.Verify(writer => writer.WriteAsync(2), Times.Once);
            mockStreamWriter.Verify(writer => writer.WriteAsync(It.IsAny<int>()), Times.Exactly(2));
        }
        [Fact]
        public async Task WriteTo_ShouldHandleMultipleSubjectsConcurrentOnNext()
        {
            // Arrange
            var mockStreamWriter = new Mock<IAsyncStreamWriter<string>>();
            const int numberOfSubjects = 99;
            int onnextTimes = 0;
            var subjects = Enumerable.Range(1, numberOfSubjects).Select(no => new MockSubject<string>(no));

            var tasks 
                = 
            subjects
            .Select(mockSub =>
            {
                var disposable = mockStreamWriter.Object.WriteTo(mockSub.Subject);
                return (mockSub, disposable);
            })
            .Select((tuple) =>
            {
                onnextTimes++;
                return Task.Run(async () =>
                {
                    tuple.mockSub.Subject.OnNext($"{tuple.mockSub.Number}:{tuple.mockSub.Id}");
                    await Task.Delay(100);
                    tuple.mockSub.Subject.OnNext(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    await Task.Delay(100);
                    tuple.mockSub.Subject.OnNext(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    await Task.Delay(100);
                    tuple.mockSub.Subject.OnNext(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    await Task.Delay(100);
                    tuple.mockSub.Subject.OnCompleted();
                    tuple.disposable.Dispose();
                });
            }).ToList();

            // Act
            await Task.WhenAll(tasks);
            await Task.Delay(1000);
            _testOutputHelper.WriteLine($"onnextTimes:{onnextTimes}");

            // Assert
            Assert.Equal(numberOfSubjects, onnextTimes);
            mockStreamWriter.Verify(writer => writer.WriteAsync(It.IsAny<string>()), Times.Exactly(numberOfSubjects * 4));
        }

    }
}
