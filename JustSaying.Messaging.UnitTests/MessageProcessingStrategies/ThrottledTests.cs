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
        public void ChangeMaxWorkersAtRuntime_TheChangeIsApplied()
        {
            Func<int> maxAllowedMessagesInFlight = Substitute.For<Func<int>>();
            maxAllowedMessagesInFlight().Returns(100);
            var messageProcessingStrategy = new Throttled(maxAllowedMessagesInFlight, _fakeMonitor);

            Assert.That(messageProcessingStrategy.MaxWorkers, Is.EqualTo(100));
            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(100));

            maxAllowedMessagesInFlight().Returns(90);

            Assert.That(messageProcessingStrategy.MaxWorkers, Is.EqualTo(90));
            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(90));
        }

        [Test]
        public void MaxWorkers_StartsAtCapacity()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);

            Assert.That(messageProcessingStrategy.MaxWorkers, Is.EqualTo(123));
        }

        [Test]
        public void AvailableWorkers_StartsAtCapacity()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);

            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(123));
        }

        [Test]
        public async Task WhenATasksIsAdded_MaxWorkersIsUnaffected()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            messageProcessingStrategy.ProcessMessage(() => tcs.Task);

            Assert.That(messageProcessingStrategy.MaxWorkers, Is.EqualTo(123));

            await AllowTasksToComplete(tcs);
        }

        [Test]
        public async Task WhenATasksIsAdded_AvailableWorkersIsDecremented()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            messageProcessingStrategy.ProcessMessage(() => tcs.Task);

            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(122));
            await AllowTasksToComplete(tcs);
        }

        [Test]
        public async Task WhenATaskCompletes_AvailableWorkersIsIncremented()
        {
            var messageProcessingStrategy = new Throttled(3, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            messageProcessingStrategy.ProcessMessage(() => tcs.Task);

            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(2));

            await AllowTasksToComplete(tcs);

            Assert.That(messageProcessingStrategy.MaxWorkers, Is.EqualTo(3));
            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(3));
        }

        [Test]
        public async Task AvailableWorkers_CanReachZero()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(() => tcs.Task);
            }

            Assert.That(messageProcessingStrategy.MaxWorkers, Is.EqualTo(capacity));
            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(0));
            await AllowTasksToComplete(tcs);
        }

        [Test]
        public async Task AvailableWorkers_CanGoToZeroAndBackToFull()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(() => tcs.Task);
            }

            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(0));

            await AllowTasksToComplete(tcs);

            Assert.That(messageProcessingStrategy.AvailableWorkers, Is.EqualTo(capacity));
        }

        [Test]
        public async Task AvailableWorkers_IsNeverNegative()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();


            for (int i = 0; i < capacity; i++)
            {
                messageProcessingStrategy.ProcessMessage(() => tcs.Task);
                Assert.That(messageProcessingStrategy.AvailableWorkers, Is.GreaterThanOrEqualTo(0));
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