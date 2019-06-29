using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
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
        private static readonly TimeSpan MinTaskDuration = TimeSpan.FromMilliseconds(100);
        private const int TaskDurationVariance = 20;

        private const int MaxAmazonBatchSize = 10;
        private static readonly TimeSpan StartTimeout = Timeout.InfiniteTimeSpan;

        [Theory]
        [InlineData(1, 20)]
        [InlineData(2, 20)]
        [InlineData(10, 20)]
        [InlineData(20, 20)]
        public async Task SimulatedListenLoop_ProcessedAllMessages(
            int numberOfMessagesToProcess,
            int maxWorkers)
        {
            var fakeMonitor = Substitute.For<IMessageMonitor>();
            var logger = Substitute.For<ILogger>();
            var messageProcessingStrategy = new Throttled(maxWorkers, StartTimeout, fakeMonitor, logger);
            var counter = new ThreadSafeCounter();

            var watch = Stopwatch.StartNew();

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
            var logger = Substitute.For<ILogger>();
            var messageProcessingStrategy = new Throttled(capacity, TimeSpan.FromTicks(1), fakeMonitor, logger);
            var counter = new ThreadSafeCounter();
            var tcs = new TaskCompletionSource<bool>();

            for (int i = 0; i < capacity; i++)
            {
                (await messageProcessingStrategy.StartWorkerAsync(
                    async () => await tcs.Task,
                    CancellationToken.None)).ShouldBeTrue();
            }

            messageProcessingStrategy.AvailableWorkers.ShouldBe(0);

            for (int i = 0; i < messageCount - capacity; i++)
            {
                (await messageProcessingStrategy.StartWorkerAsync(() => Task.CompletedTask, CancellationToken.None)).ShouldBeFalse();
            }

            messageProcessingStrategy.AvailableWorkers.ShouldBe(0);

            tcs.SetResult(true);

            (await messageProcessingStrategy.WaitForAvailableWorkerAsync()).ShouldBeGreaterThan(0);

            fakeMonitor.Received().IncrementThrottlingStatistic();
            fakeMonitor.Received().HandleThrottlingTime(Arg.Any<TimeSpan>());
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(5, 10)]
        [InlineData(9, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 50)]
        [InlineData(49, 50)]
        [InlineData(50, 50)]
        public async Task SimulatedListenLoop_WhenThrottlingDoesNotOccur_DoNotCallMessageMonitor(int messageCount, int capacity)
        {
            messageCount.ShouldBeLessThanOrEqualTo(capacity,
                "To avoid throttling, message count must be not be over capacity");

            var fakeMonitor = Substitute.For<IMessageMonitor>();
            var logger = Substitute.For<ILogger>();
            var messageProcessingStrategy = new Throttled(capacity, Timeout.InfiniteTimeSpan, fakeMonitor, logger);
            var counter = new ThreadSafeCounter();

            var actions = BuildFakeIncomingMessages(messageCount, counter);

            await ListenLoopExecuted(actions, messageProcessingStrategy);

            if (messageCount == capacity)
            {
                // Throttles once in call to WaitForAvailableWorkerAsync
                fakeMonitor.Received(1).IncrementThrottlingStatistic();
            }
            else
            {
                fakeMonitor.DidNotReceive().IncrementThrottlingStatistic();
            }
        }

        private static async Task ListenLoopExecuted(
            Queue<Func<Task>> actions,
            IMessageProcessingStrategy messageProcessingStrategy)
        {
            var initalActionCount = (double)actions.Count;
            var timeout = MinTaskDuration + TimeSpan.FromMilliseconds(initalActionCount / 100);
            var stopwatch = Stopwatch.StartNew();

            while (actions.Any())
            {
                var batch = GetFromFakeSnsQueue(actions, messageProcessingStrategy.MaxWorkers);

                if (batch.Count < 1)
                {
                    break;
                }

                foreach (var action in batch)
                {
                    (await messageProcessingStrategy.StartWorkerAsync(action, CancellationToken.None)).ShouldBeTrue();
                }

                messageProcessingStrategy.MaxWorkers.ShouldBeGreaterThanOrEqualTo(0);

                (await messageProcessingStrategy.WaitForAvailableWorkerAsync()).ShouldBeGreaterThan(0);
                messageProcessingStrategy.MaxWorkers.ShouldBeGreaterThan(0);

                stopwatch.Elapsed.ShouldBeLessThanOrEqualTo((timeout * 3),
                    $"ListenLoopExecuted took longer than timeout of {timeout}, with {actions.Count} of {initalActionCount} messages remaining.");
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
                var duration = MinTaskDuration + TimeSpan.FromMilliseconds(random.Next(TaskDurationVariance));

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
