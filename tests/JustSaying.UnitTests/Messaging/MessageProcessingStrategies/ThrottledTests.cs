using System;
using System.Collections.Concurrent;
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

        [Fact]
        public async Task ProcessMessagesSequentially_True_Processes_Messages_One_By_One()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 1,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = TimeSpan.FromSeconds(5),
                ProcessMessagesSequentially = true,
            };

            var strategy = new Throttled(options);

            int count = 0;
            object syncRoot = new object();

            Task DoWork()
            {
                if (Monitor.TryEnter(syncRoot))
                {
                    Interlocked.Increment(ref count);
                    Monitor.Exit(syncRoot);
                }
                else
                {
                    throw new InvalidOperationException("Failed to acquire lock as the thread was different.");
                }

                return Task.CompletedTask;
            }

            // Act
            int loopCount = 100_000;

            Monitor.Enter(syncRoot);

            for (int i = 0; i < loopCount; i++)
            {
                await strategy.StartWorkerAsync(DoWork, CancellationToken.None);
            }

            Monitor.Exit(syncRoot);

            // Assert
            count.ShouldBe(loopCount);
        }

        [Fact]
        public async Task ProcessMessagesSequentially_False_Processes_Messages_In_Parallel()
        {
            // Arrange
            var options = new ThrottledOptions()
            {
                MaxConcurrency = 100,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = Timeout.InfiniteTimeSpan,
                ProcessMessagesSequentially = false,
            };

            var strategy = new Throttled(options);

            long count = 0;
            var threadsSeen = new ConcurrentBag<int>();

            Task DoWork()
            {
                threadsSeen.Add(Thread.CurrentThread.ManagedThreadId);
                Interlocked.Increment(ref count);

                return Task.CompletedTask;
            }

            int loopCount = 1_000;

            // Act
            for (int i = 0; i < loopCount; i++)
            {
                await strategy.StartWorkerAsync(DoWork, CancellationToken.None);
            }

            bool allWorkDone = SpinWait.SpinUntil(() => Interlocked.Read(ref count) >= 1000, TimeSpan.FromSeconds(10));

            // Assert
            allWorkDone.ShouldBeTrue();
            count.ShouldBe(loopCount);
            threadsSeen.Distinct().Count().ShouldBeGreaterThan(1);
        }

        [Fact]
        public async Task Parallel_Processing_Does_Not_Exceed_Concurrency()
        {
            // Arrange
            int maxConcurrency = 10;

            var options = new ThrottledOptions()
            {
                MaxConcurrency = maxConcurrency,
                Logger = _logger,
                MessageMonitor = _monitor,
                StartTimeout = Timeout.InfiniteTimeSpan,
                ProcessMessagesSequentially = false,
            };

            var strategy = new Throttled(options);

            long workDone = 0;
            int loopCount = 1_000;
            bool allWorkDone;

            using (var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency))
            {
                async Task DoWork()
                {
                    if (!(await semaphore.WaitAsync(0)))
                    {
                        throw new InvalidOperationException("More workers are doing work than expected.");
                    }

                    Interlocked.Increment(ref workDone);
                    semaphore.Release();
                }

                // Act
                for (int i = 0; i < loopCount; i++)
                {
                    await strategy.StartWorkerAsync(DoWork, CancellationToken.None);
                }

                allWorkDone = SpinWait.SpinUntil(() => Interlocked.Read(ref workDone) >= 1000, TimeSpan.FromSeconds(10));
            }

            // Assert
            allWorkDone.ShouldBeTrue();
            workDone.ShouldBe(loopCount);
        }

        private static async Task AllowTasksToComplete(TaskCompletionSource<object> doneSignal)
        {
            doneSignal.SetResult(null);
            await Task.Delay(100);
        }
    }
}
