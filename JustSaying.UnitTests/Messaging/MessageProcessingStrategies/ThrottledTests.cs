using System;
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
    public class ThrottledTests
    {
        private readonly IMessageMonitor _monitor;
        private readonly ILogger _logger;

        public ThrottledTests()
        {
            _monitor = Substitute.For<IMessageMonitor>();
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void MaxConcurrency_StartsAtMaxConcurrency()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 123,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                UseThreadPool = true,
            };

            // Act
            var messageProcessingStrategy = new Throttled(options);

            // Assert
            messageProcessingStrategy.MaxConcurrency.ShouldBe(123);
        }

        [Fact]
        public void AvailableWorkers_StartsAtMaxConcurrency()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 123,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                UseThreadPool = true,
            };

            // Act
            var messageProcessingStrategy = new Throttled(options);

            // Assert
            messageProcessingStrategy.AvailableWorkers.ShouldBe(123);
        }

        [Fact]
        public async Task WhenATasksIsAdded_MaxConcurrencyIsUnaffected()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 123,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                UseThreadPool = true,
            };

            var messageProcessingStrategy = new Throttled(options);
            var tcs = new TaskCompletionSource<object>();

            // Act
            (await messageProcessingStrategy.StartWorkerAsync(() => tcs.Task, CancellationToken.None)).ShouldBeTrue();

            // Assert
            messageProcessingStrategy.MaxConcurrency.ShouldBe(123);

            await AllowTasksToComplete(tcs);
        }

        [Fact]
        public async Task WhenATasksIsAdded_AvailableWorkersIsDecremented()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 123,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                UseThreadPool = true,
            };

            var messageProcessingStrategy = new Throttled(options);
            var tcs = new TaskCompletionSource<object>();

            // Act
            (await messageProcessingStrategy.StartWorkerAsync(() => tcs.Task, CancellationToken.None)).ShouldBeTrue();

            // Assert
            messageProcessingStrategy.AvailableWorkers.ShouldBe(122);
            await AllowTasksToComplete(tcs);
        }

        [Fact]
        public async Task WhenATaskCompletes_AvailableWorkersIsIncremented()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 3,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                UseThreadPool = true,
            };

            var messageProcessingStrategy = new Throttled(options);
            var tcs = new TaskCompletionSource<object>();

            // Act
            (await messageProcessingStrategy.StartWorkerAsync(() => tcs.Task, CancellationToken.None)).ShouldBeTrue();

            // Assert
            messageProcessingStrategy.AvailableWorkers.ShouldBe(2);

            await AllowTasksToComplete(tcs);

            messageProcessingStrategy.MaxConcurrency.ShouldBe(3);
            messageProcessingStrategy.AvailableWorkers.ShouldBe(3);
        }

        [Fact]
        public async Task AvailableWorkers_CanReachZero()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 10,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                UseThreadPool = true,
            };

            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(options);
            var tcs = new TaskCompletionSource<object>();

            // Act
            for (int i = 0; i < capacity; i++)
            {
                (await messageProcessingStrategy.StartWorkerAsync(() => tcs.Task, CancellationToken.None)).ShouldBeTrue();
            }

            // Assert
            messageProcessingStrategy.MaxConcurrency.ShouldBe(capacity);
            messageProcessingStrategy.AvailableWorkers.ShouldBe(0);
            await AllowTasksToComplete(tcs);
        }

        [Fact]
        public async Task AvailableWorkers_CanGoToZeroAndBackToFull()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 10,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                UseThreadPool = true,
            };

            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(options);
            var tcs = new TaskCompletionSource<object>();

            // Act
            for (int i = 0; i < capacity; i++)
            {
                (await messageProcessingStrategy.StartWorkerAsync(() => tcs.Task, CancellationToken.None)).ShouldBeTrue();
            }

            // Assert
            messageProcessingStrategy.AvailableWorkers.ShouldBe(0);

            await AllowTasksToComplete(tcs);

            messageProcessingStrategy.AvailableWorkers.ShouldBe(capacity);
        }

        [Fact]
        public async Task AvailableWorkers_IsNeverNegative()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 10,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                UseThreadPool = true,
            };

            const int capacity = 10;
            var messageProcessingStrategy = new Throttled(options);
            var tcs = new TaskCompletionSource<object>();

            // Act
            for (int i = 0; i < capacity; i++)
            {
                (await messageProcessingStrategy.StartWorkerAsync(() => tcs.Task, CancellationToken.None)).ShouldBeTrue();
                messageProcessingStrategy.AvailableWorkers.ShouldBeGreaterThanOrEqualTo(0);
            }

            // Assert
            (await messageProcessingStrategy.StartWorkerAsync(() => tcs.Task, CancellationToken.None)).ShouldBeFalse();
            messageProcessingStrategy.AvailableWorkers.ShouldBe(0);

            await AllowTasksToComplete(tcs);
        }

        private static async Task AllowTasksToComplete(TaskCompletionSource<object> doneSignal)
        {
            doneSignal.SetResult(null);
            await Task.Delay(100);
        }
    }
}
