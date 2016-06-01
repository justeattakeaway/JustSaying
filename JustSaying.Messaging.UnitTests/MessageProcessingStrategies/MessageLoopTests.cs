using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.MessageProcessingStrategies
{
    public class ThreadSafeCounter
    {
        private int _count = 0;

        public void Increment()
        {
            Interlocked.Increment(ref _count);
        }

        public int Count {  get { return _count; } }
    }

    [TestFixture]
    public class MessageLoopTests
    {
        private const int MinTaskDuration = 10;
        private const int TaskDurationVariance = 20;

        private const int ConcurrencyLevel = 20;
        private const int MaxAmazonBatchSize = 10;

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        [TestCase(20)]
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

            await Task.Yield();
            await Task.Delay(2000);
            await Task.Yield();

            Assert.That(counter.Count, Is.EqualTo(numberOfMessagesToProcess));
        }

        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(6, 5)]
        [TestCase(11, 10)]
        [TestCase(100, 90)]
        [TestCase(30, 20)]
        [TestCase(1000, 900)]
        public async Task SimulatedListenLoop_WhenThrottlingOccurs_CallsMessageMonitor(int messageCount, int capacity)
        {
            Assert.That(messageCount, Is.GreaterThan(capacity), "To cause throttling, message count must be over capacity");

            var fakeMonitor = Substitute.For<IMessageMonitor>();
            var messageProcessingStrategy = new Throttled(capacity, fakeMonitor);
            var counter = new ThreadSafeCounter();

            var actions = BuildFakeIncomingMessages(messageCount, counter);

            await ListenLoopExecuted(actions, messageProcessingStrategy);

            fakeMonitor.Received().IncrementThrottlingStatistic();
            fakeMonitor.Received().HandleThrottlingTime(Arg.Any<long>());
        }

        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(5, 10)]
        [TestCase(10, 50)]
        [TestCase(50, 50)]
        public async Task SimulatedListenLoop_WhenThrottlingDoesNotOccur_DoNotCallMessageMonitor(int messageCount, int capacity)
        {
            Assert.That(messageCount, Is.LessThanOrEqualTo(capacity), "To avoid throttling, message count must be not be over capacity");

            var fakeMonitor = Substitute.For<IMessageMonitor>();
            var messageProcessingStrategy = new Throttled(capacity, fakeMonitor);
            var counter = new ThreadSafeCounter();

            var actions = BuildFakeIncomingMessages(messageCount, counter);

            await ListenLoopExecuted(actions, messageProcessingStrategy);

            fakeMonitor.DidNotReceive().IncrementThrottlingStatistic();
        }

        private async Task ListenLoopExecuted(Queue<Func<Task>> actions,
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
                    messageProcessingStrategy.ProcessMessage(action);
                }

                if (!actions.Any())
                {
                    break;
                }

                Assert.That(messageProcessingStrategy.AvailableWorkers, Is.GreaterThanOrEqualTo(0));
                await messageProcessingStrategy.AwaitAtLeastOneWorkerToComplete();
                Assert.That(messageProcessingStrategy.AvailableWorkers, Is.GreaterThan(0));

                if (stopwatch.Elapsed > timeout)
                {
                    var message = string.Format("ListenLoopExecuted took longer than timeout of {0}s, with {1} of {2} messages remaining",
                        timeoutSeconds, actions.Count, initalActionCount);
                    Assert.Fail(message);
                }
            }
        }

        private IList<Func<Task>> GetFromFakeSnsQueue(Queue<Func<Task>> actions, int requestedBatchSize)
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

        private Queue<Func<Task>> BuildFakeIncomingMessages(int numberOfMessagesToCreate, ThreadSafeCounter counter)
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