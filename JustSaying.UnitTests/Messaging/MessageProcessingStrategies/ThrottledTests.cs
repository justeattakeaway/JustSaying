using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.MessageProcessingStrategies
{
    public class ThrottledTests
    {
        private readonly IMessageMonitor _fakeMonitor;

        public ThrottledTests()
        {
            _fakeMonitor = Substitute.For<IMessageMonitor>();
        }

        [Fact]
        public void MaxWorkers_StartsAtCapacity()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);

            messageProcessingStrategy.MaxWorkers.ShouldBe(123);
        }

        [Fact]
        public void AvailableWorkers_StartsAtCapacity()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);

            messageProcessingStrategy.AvailableWorkers.ShouldBe(123);
        }

        [Fact]
        public async Task WhenATasksIsAdded_MaxWorkersIsUnaffected()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            await messageProcessingStrategy.StartWorker(() => tcs.Task, CancellationToken.None);

            messageProcessingStrategy.MaxWorkers.ShouldBe(123);

            await AllowTasksToComplete(tcs);
        }

        [Fact]
        public async Task WhenATasksIsAdded_AvailableWorkersIsDecremented()
        {
            var messageProcessingStrategy = new Throttled(123, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            await messageProcessingStrategy.StartWorker(() => tcs.Task, CancellationToken.None);

            messageProcessingStrategy.AvailableWorkers.ShouldBe(122);
            await AllowTasksToComplete(tcs);
        }

        [Fact]
        public async Task WhenATaskCompletes_AvailableWorkersIsIncremented()
        {
            var messageProcessingStrategy = new Throttled(3, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            await messageProcessingStrategy.StartWorker(() => tcs.Task, CancellationToken.None);

            messageProcessingStrategy.AvailableWorkers.ShouldBe(2);

            await AllowTasksToComplete(tcs);

            messageProcessingStrategy.MaxWorkers.ShouldBe(3);
            messageProcessingStrategy.AvailableWorkers.ShouldBe(3);
        }

        [Fact]
        public async Task AvailableWorkers_CanReachZero()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            for (int i = 0; i < capacity; i++)
            {
                await messageProcessingStrategy.StartWorker(() => tcs.Task, CancellationToken.None);
            }

            messageProcessingStrategy.MaxWorkers.ShouldBe(capacity);
            messageProcessingStrategy.AvailableWorkers.ShouldBe(0);
            await AllowTasksToComplete(tcs);
        }

        [Fact]
        public async Task AvailableWorkers_CanGoToZeroAndBackToFull()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();

            for (int i = 0; i < capacity; i++)
            {
                await messageProcessingStrategy.StartWorker(() => tcs.Task, CancellationToken.None);
            }

            messageProcessingStrategy.AvailableWorkers.ShouldBe(0);

            await AllowTasksToComplete(tcs);

            messageProcessingStrategy.AvailableWorkers.ShouldBe(capacity);
        }

        [Fact]
        public async Task AvailableWorkers_IsNeverNegative()
        {
            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(capacity, _fakeMonitor);
            var tcs = new TaskCompletionSource<object>();


            for (int i = 0; i < capacity; i++)
            {
                await messageProcessingStrategy.StartWorker(() => tcs.Task, CancellationToken.None);
                messageProcessingStrategy.AvailableWorkers.ShouldBeGreaterThanOrEqualTo(0);
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
