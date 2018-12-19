using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.MessageProcessingStrategies
{
    public class ThreadSafeCounter
    {
        private int _count;

        public void Increment()
        {
            Interlocked.Increment(ref _count);
        }

        public int Count => _count;
    }

    public class MessageLoopTests
    {
        private const int MinTaskDuration = 10;
        private const int TaskDurationVariance = 20;

        private const int ConcurrencyLevel = 20;
        private const int MaxAmazonBatchSize = 10;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        [InlineData(20)]
        public async Task SimulatedListenLoop_ProcessedAllMessages(int numberOfMessagesToProcess)
        {
            var fakeMonitor = Substitute.For<IMessageMonitor>();
            var messageProcessingStrategy = new Throttled(ConcurrencyLevel, fakeMonitor);
            var counter = new ThreadSafeCounter();

            var watch = new Stopwatch();
            watch.Start();

            var actions = BuildFakeIncomingMessages(numberOfMessagesToProcess, counter);
            await ListenLoopExecuted(actions, messageProcessingStrategy);

            watch.Stop();

            await Task.Delay(2000);

            counter.Count.ShouldBe(numberOfMessagesToProcess);
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(3, 2)]
        [InlineData(6, 5)]
        [InlineData(11, 10)]
        [InlineData(100, 90)]
        [InlineData(30, 20)]
        [InlineData(1000, 900)]
        public async Task SimulatedListenLoop_WhenThrottlingOccurs_CallsMessageMonitor(int messageCount, int capacity)
        {
            messageCount.ShouldBeGreaterThan(capacity, "To cause throttling, message count must be over capacity");

            var fakeMonitor = Substitute.For<IMessageMonitor>();
            var messageProcessingStrategy = new Throttled(capacity, fakeMonitor);
            var counter = new ThreadSafeCounter();

            var actions = BuildFakeIncomingMessages(messageCount, counter);

            await ListenLoopExecuted(actions, messageProcessingStrategy);

            fakeMonitor.Received().IncrementThrottlingStatistic();
            fakeMonitor.Received().HandleThrottlingTime(Arg.Any<TimeSpan>());
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(5, 10)]
        [InlineData(10, 50)]
        [InlineData(50, 50)]
        public async Task SimulatedListenLoop_WhenThrottlingDoesNotOccur_DoNotCallMessageMonitor(int messageCount, int capacity)
        {
            messageCount.ShouldBeLessThanOrEqualTo(capacity,
                "To avoid throttling, message count must be not be over capacity");

            var fakeMonitor = Substitute.For<IMessageMonitor>();
            var messageProcessingStrategy = new Throttled(capacity, fakeMonitor);
            var counter = new ThreadSafeCounter();

            var actions = BuildFakeIncomingMessages(messageCount, counter);

            await ListenLoopExecuted(actions, messageProcessingStrategy);

            fakeMonitor.DidNotReceive().IncrementThrottlingStatistic();
        }

        private static async Task ListenLoopExecuted(Queue<Func<Task>> actions,
            IMessageProcessingStrategy messageProcessingStrategy)
        {
            var initalActionCount = actions.Count;
            var timeoutSeconds = 10 + (initalActionCount / 100);
            var timeout = new TimeSpan(0, 0, timeoutSeconds);
            var stopwatch = Stopwatch.StartNew();

            while (actions.Any())
            {
                var batch = GetFromFakeSnsQueue(actions, messageProcessingStrategy.AvailableWorkers);

                foreach (var action in batch)
                {
                    await messageProcessingStrategy.StartWorker(action, CancellationToken.None);
                }

                if (!actions.Any())
                {
                    break;
                }

                messageProcessingStrategy.AvailableWorkers.ShouldBeGreaterThanOrEqualTo(0);
                await messageProcessingStrategy.WaitForAvailableWorkers();
                messageProcessingStrategy.AvailableWorkers.ShouldBeGreaterThan(0);

                stopwatch.Elapsed.ShouldBeLessThanOrEqualTo(timeout,
                    $"ListenLoopExecuted took longer than timeout of {timeoutSeconds}s, with {actions.Count} of {initalActionCount} messages remaining");
            }
        }

        private static IList<Func<Task>> GetFromFakeSnsQueue(Queue<Func<Task>> actions, int requestedBatchSize)
        {
            var batchSize = Math.Min(requestedBatchSize, MaxAmazonBatchSize);
            batchSize = Math.Min(batchSize, actions.Count);

            var batch = new List<Func<Task>>();

            for (var i = 0; i < batchSize; i++)
            {
                batch.Add(actions.Dequeue());
            }
            return batch;
        }

        private static Queue<Func<Task>> BuildFakeIncomingMessages(int numberOfMessagesToCreate, ThreadSafeCounter counter)
        {
            var random = new Random();
            var actions = new Queue<Func<Task>>();
            for (var i = 0; i != numberOfMessagesToCreate; i++)
            {
                var duration = MinTaskDuration + random.Next(TaskDurationVariance);

                var action = new Func<Task>(async () =>
                    {
                        await Task.Delay(duration);
                        counter.Increment();
                    });
                actions.Enqueue(action);
            }

            return actions;
        }
    }
}
