using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using JustEat.Simples.NotificationStack.Messaging.MessageProcessingStrategies;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;

namespace AwsTools.UnitTests.MessageProcessingStrategies
{
    [TestFixture]
    public class ThrottledTests
    {
        private int _concurrencyLevel;
        private Throttled _messageProcessingStrategy;
        private int _actionsProcessed;
        private int _fakeAmazonBatchSize;
        private IMessageMonitor _fakeMonitor;

        [SetUp]
        public void SetUp()
        {
            _fakeMonitor = Substitute.For<IMessageMonitor>();
            _fakeAmazonBatchSize = 10;
            _concurrencyLevel = 20;
            _messageProcessingStrategy = new Throttled(_concurrencyLevel, _fakeAmazonBatchSize, _fakeMonitor);
            _actionsProcessed = 0;
        }

        [Test]
        public void ChangeMaxAllowedMessagesInFlightAtRuntime_TheChangeIsApplied()
        {
            var MaxAllowedMessagesInFlight = Substitute.For<Func<int>>();
            MaxAllowedMessagesInFlight().Returns(100);
            _messageProcessingStrategy = new Throttled(MaxAllowedMessagesInFlight, 10, _fakeMonitor);

            MaxAllowedMessagesInFlight().Returns(90);

            Assert.That(_messageProcessingStrategy.BlockingThreshold, Is.EqualTo(90 - 10));

        }

        [TestCase(0, 10, 1)]
        [TestCase(9, 10, 1)]
        [TestCase(10, 10, 1)]
        [TestCase(11, 10, 1)]
        [TestCase(12, 10, 2)]
        public void Ctor_WhenMaxAllowedMessagesInFlightIsNearToBatchSize_BlockingThresholdIsNeverNegative(int maxAllowedMessagesInFlight, int maxBatchSize, int expectedBlockingThreshold)
        {
            _messageProcessingStrategy = new Throttled(maxAllowedMessagesInFlight, maxBatchSize, _fakeMonitor);

            Assert.That(_messageProcessingStrategy.BlockingThreshold, Is.EqualTo(expectedBlockingThreshold));
        }


        [TestCase(1000)]
        [TestCase(10000)]
        public void SimulatedListenLoop_ProcessedAllMessages(int numberOfMessagesToProcess)
        {
            var watch = new Stopwatch();
            watch.Start();
            var actions = BuildFakeIncomingMessages(numberOfMessagesToProcess);

            ListenLoopExecuted(actions);

            watch.Stop();
            Thread.Sleep(2000);

            Assert.That(_actionsProcessed, Is.EqualTo(numberOfMessagesToProcess));
            Debug.WriteLine("Took " + watch.Elapsed + " to process " + numberOfMessagesToProcess + " messages.");
        }


        [Test]
        public void SimulatedListenLoop_WhenThrottlingOccurs_CallsMessageMonitor()
        {
            var actions = BuildFakeIncomingMessages(50);
            _messageProcessingStrategy = new Throttled(20, _fakeAmazonBatchSize, _fakeMonitor);

            ListenLoopExecuted(actions);

            _fakeMonitor.Received().IncrementThrottlingStatistic();
        }

        private void ListenLoopExecuted(Queue<Action> actions)
        {
            while (actions.Any())
            {
                _messageProcessingStrategy.BeforeGettingMoreMessages();
                var batch = GetFromFakeSnsQueue(actions);

                foreach (var action in batch)
                {
                    _messageProcessingStrategy.ProcessMessage(action);
                }
            }
        }

        private IEnumerable<Action> GetFromFakeSnsQueue(Queue<Action> actions)
        {
            var batch = new List<Action>();
            for (var i = 0; i != _fakeAmazonBatchSize; i++)
            {
                batch.Add(actions.Dequeue());
            }
            return batch;
        }

        private Queue<Action> BuildFakeIncomingMessages(int numberOfMessagesToCreate, int maximumDurationASingleMessageWillTakeToExecute = 10)
        {
            var actions = new Queue<Action>();
            for (var i = 0; i != numberOfMessagesToCreate; i++)
            {
                var random = new Random();
                var action = new Action(() =>
                {
                    Thread.Sleep(random.Next(maximumDurationASingleMessageWillTakeToExecute));
                    Interlocked.Increment(ref _actionsProcessed);
                });
                actions.Enqueue(action);
            }

            return actions;
        }
    }
}