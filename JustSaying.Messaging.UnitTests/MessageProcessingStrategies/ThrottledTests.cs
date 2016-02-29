using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.MessageProcessingStrategies
{
    [TestFixture]
    public class ThrottledTests
    {
        private IMessageMonitor _fakeMonitor;

        [SetUp]
        public void SetUp()
        {
            _fakeMonitor = Substitute.For<IMessageMonitor>();
        }

        [Test]
        public void ChangeMaxAllowedMessagesInFlightAtRuntime_TheChangeIsApplied()
        {
            Func<int> maxAllowedMessagesInFlight = Substitute.For<Func<int>>();
            maxAllowedMessagesInFlight().Returns(100);
            var messageProcessingStrategy = new Throttled(maxAllowedMessagesInFlight, _fakeMonitor);

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(100));

            maxAllowedMessagesInFlight().Returns(90);

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(90));
        }

        [Test]
        public void FreeTaskCountStartsAtCapacity()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);
            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(123));
        }

        [Test]
        public void WhenATasksIsAddedTheFreeTaskCountIsDecremented()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);
            var tcs = new TaskCompletionSource<bool>();

            messageProcessingStrategy.ProcessMessage(async () => await tcs.Task);

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(122));
        }

        [Test]
        public async Task WhenATaskCompletesTheFreeTaskCountIsIncremented()
        {
            var messageProcessingStrategy = new Throttled(3, _fakeMonitor);
            var tcs = new TaskCompletionSource<bool>();

            messageProcessingStrategy.ProcessMessage(async () => await tcs.Task);

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(2));

            await AllowTasksToComplete(tcs);

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(3));
        }

        [Test]
        public void FreeTaskCountCanReachZero()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<bool>();

            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(async () => await tcs.Task);
            }

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(0));
            tcs.SetResult(true);
        }

        [Test]
        public async Task FreeTaskCountCanGoToZeroAndBackToFull()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<bool>();

            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(async () => await tcs.Task);
            }

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(0));

            await AllowTasksToComplete(tcs);

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(capacity));
        }

        [Test]
        public void FreeTaskCountIsNeverNegative()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<bool>();


            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(async () => await tcs.Task);
                Assert.That(messageProcessingStrategy.FreeTasks, Is.GreaterThanOrEqualTo(0));
            }
            tcs.SetResult(true);
        }

        private static async Task AllowTasksToComplete(TaskCompletionSource<bool> tcs)
        {
            tcs.SetResult(true);
            await Task.Delay(250);
        }
    }
}