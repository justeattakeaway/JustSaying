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

            messageProcessingStrategy.ProcessMessage(() => tcs.Task.Wait());

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(122));
        }

        [Test]
        public async Task WhenATaskCompletesTheFreeTaskCountIsIncremented()
        {
            var messageProcessingStrategy = new Throttled(3, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            messageProcessingStrategy.ProcessMessage(() => tcs.Task.Wait());

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(2));

            await AllowTasksToComplete(tcs);

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(3));
        }

        [Test]
        public async Task FreeTaskCountCanReachZero()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(() => tcs.Task.Wait());
            }
            
            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(0));
            await AllowTasksToComplete(tcs);
        }

        [Test]
        public async Task FreeTaskCountCanGoToZeroAndBackToFull()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(() => tcs.Task.Wait());
            }

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(0));

            await AllowTasksToComplete(tcs);

            Assert.That(messageProcessingStrategy.FreeTasks, Is.EqualTo(capacity));
        }

        [Test]
        public async Task FreeTaskCountIsNeverNegative()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();


            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(() => tcs.Task.Wait());
                Assert.That(messageProcessingStrategy.FreeTasks, Is.GreaterThanOrEqualTo(0));
            }

            await AllowTasksToComplete(tcs);
        }

        private static async Task AllowTasksToComplete(TaskCompletionSource<object> doneSignal)
        {
            doneSignal.SetResult(null);
            await Task.Yield();
            await Task.Delay(100);
        }
    }
}