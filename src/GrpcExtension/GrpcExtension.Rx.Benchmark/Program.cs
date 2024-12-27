// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GrpcExtension.Rx.Test.Mocks;
using GrpcExtension.Rx;
using System.Reactive.Subjects;

namespace GrpcExtension.Rx.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<StreamWriterBenchmarks>();
            Console.Read();
        }
    }
    [MemoryDiagnoser]
    public class StreamWriterBenchmarks
    {
        private MockAsyncStreamWriter<int> _writer;
        private Subject<int> _subject;
        private IDisposable _subscription;

        [GlobalSetup]
        public void Setup()
        {
            _writer = new MockAsyncStreamWriter<int>();
            _subject = new Subject<int>();
            _subscription = _writer.WriteTo(_subject);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _subscription?.Dispose();
            _subject?.Dispose();
        }

        [Benchmark]
        public async Task WriteItems_Small()
        {
            for (int i = 0; i < 100; i++)
            {
                _subject.OnNext(i);
            }
            await Task.Delay(100); // 给时间让操作完成
        }

        [Benchmark]
        public async Task WriteItems_Medium()
        {
            for (int i = 0; i < 1000; i++)
            {
                _subject.OnNext(i);
            }
            await Task.Delay(100);
        }

        [Benchmark]
        public async Task WriteItems_Large()
        {
            for (int i = 0; i < 10000; i++)
            {
                _subject.OnNext(i);
            }
            await Task.Delay(100);
        }
    }
}

