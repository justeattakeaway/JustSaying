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
    [TestFixture, Ignore("Fails on appveyor")]
    public class MessageLoopTests
    {
        private const int MinTaskDuration = 10;
        private const int TaskDurationVariance = 20;

        private const int ConcurrencyLevel = 20;
        private const int MaxAmazonBatchSize = 10;

        private int _actionsProcessed;

        [SetUp]
        public void SetUp()
        {
            _actionsProcessed = 0;
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task SimulatedListenLoop_ProcessedAllMessages(int numberOfMessagesToProcess)
        {
            var fakeMonitor = Substitute.For<IMessageMonitor>();
            var messageProcessingStrategy = new Throttled(ConcurrencyLevel, fakeMonitor);

            var watch = new Stopwatch();
            watch.Start();

            var actions = BuildFakeIncomingMessages(numberOfMessagesToProcess);
            await ListenLoopExecuted(actions, messageProcessingStrategy);

            watch.Stop();

            await Task.Delay(1000);

            Assert.That(_actionsProcessed, Is.EqualTo(numberOfMessagesToProcess));
            Debug.WriteLine("Took " + watch.Elapsed + " to process " + numberOfMessagesToProcess + " messages.");
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

            var actions = BuildFakeIncomingMessages(messageCount);

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

            var actions = BuildFakeIncomingMessages(messageCount);

            await ListenLoopExecuted(actions, messageProcessingStrategy);

            fakeMonitor.DidNotReceive().IncrementThrottlingStatistic();
        }

        private async Task ListenLoopExecuted(Queue<Action> actions, IMessageProcessingStrategy messageProcessingStrategy)
        {
            const int timeoutSeconds = 10;
            var timeout = new TimeSpan(0, 0, timeoutSeconds);
            var stopwatch = Stopwatch.StartNew();

            while (actions.Any())
            {
                var batch = GetFromFakeSnsQueue(actions, messageProcessingStrategy.FreeTasks);

                foreach (var action in batch)
                {
                    messageProcessingStrategy.ProcessMessage(action);
                }

                if (!actions.Any())
                {
                    break;
                }

                Assert.That(messageProcessingStrategy.FreeTasks, Is.GreaterThanOrEqualTo(0));
                await messageProcessingStrategy.AwaitAtLeastOneTaskToComplete();
                Assert.That(messageProcessingStrategy.FreeTasks, Is.GreaterThan(0));

                if (stopwatch.Elapsed > timeout)
                {
                    Assert.Fail("ListenLoopExecuted took longer than timout of " + timeoutSeconds);
                }
            }
        }

        private IList<Action> GetFromFakeSnsQueue(Queue<Action> actions, int requestedBatchSize)
        {
            var batchSize = Math.Min(requestedBatchSize, MaxAmazonBatchSize);
            batchSize = Math.Min(batchSize, actions.Count);
            Debug.WriteLine("Getting a batch of {0} for requested {1}, queue contains {2}", batchSize, requestedBatchSize, actions.Count);

            var batch = new List<Action>();

            for (var i = 0; i < batchSize; i++)
            {
                batch.Add(actions.Dequeue());
            }
            return batch;
        }

        private Queue<Action> BuildFakeIncomingMessages(int numberOfMessagesToCreate)
        {
            var random = new Random();
            var actions = new Queue<Action>();
            for (var i = 0; i != numberOfMessagesToCreate; i++)
            {
                var duration = MinTaskDuration + random.Next(TaskDurationVariance);

                var action = new Action(() =>
                    {
                         Thread.Sleep(duration);
                        Interlocked.Increment(ref _actionsProcessed);
                    });
                actions.Enqueue(action);
            }

            return actions;
        }
    }
}