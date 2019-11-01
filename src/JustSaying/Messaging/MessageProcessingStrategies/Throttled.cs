using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    /// <summary>
    /// A class representing an implementation of <see cref="IMessageProcessingStrategy"/>
    /// that throttles the number of messages that can be processed concurrently.
    /// </summary>
    public class Throttled : IMessageProcessingStrategy, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMessageMonitor _messageMonitor;
        private readonly SemaphoreSlim _semaphore;
        private readonly bool _processSequentially;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Throttled"/> class.
        /// </summary>
        /// <param name="options">The <see cref="ThrottledOptions"/> to use.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> does not specify an <see cref="ILogger"/> or <see cref="IMessageMonitor"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The concurrency specified by <paramref name="options"/> is less than one,
        /// or the start timeout specified by <paramref name="options"/> is zero or negative.
        /// </exception>
        public Throttled(ThrottledOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.MaxConcurrency < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options), options.MaxConcurrency, "The maximum concurrency value must be a positive integer.");
            }

            if (options.StartTimeout <= TimeSpan.Zero && options.StartTimeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    options.StartTimeout,
                    "The start timeout must be a positive value or Timeout.InfiniteTimeSpan.");
            }

            _messageMonitor = options.MessageMonitor ?? throw new ArgumentException($"A value for {nameof(ThrottledOptions.MessageMonitor)} must be specified.", nameof(options));
            _logger = options.Logger ?? throw new ArgumentException($"A value for {nameof(ThrottledOptions.Logger)} must be specified.", nameof(options));

            MaxConcurrency = options.MaxConcurrency;
            StartTimeout = options.StartTimeout;

            _processSequentially = options.ProcessMessagesSequentially;
            _semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Throttled"/> class.
        /// </summary>
        ~Throttled()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the available number of workers.
        /// </summary>
        public int AvailableWorkers => _semaphore.CurrentCount;

        /// <inheritdoc />
        public int MaxConcurrency { get; }

        /// <summary>
        /// Gets the timeout to use for starting a worker.
        /// </summary>
        public TimeSpan StartTimeout { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public virtual async Task<bool> StartWorkerAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            try
            {
                if (await _semaphore.WaitAsync(StartTimeout, cancellationToken).ConfigureAwait(false))
                {
                    if (_processSequentially)
                    {
                        await RunActionAsync(action).ConfigureAwait(false);
                    }
                    else
                    {
                        EnqueueAction(action);
                    }

                    return true;
                }
                else
                {
                    _messageMonitor.IncrementThrottlingStatistic();
                    _messageMonitor.HandleThrottlingTime(StartTimeout);

                    _logger.LogWarning("Message handling was throttled waiting for the semaphore for {StartTimeout}.", StartTimeout);

                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<int> WaitForAvailableWorkerAsync()
        {
            if (_semaphore.CurrentCount == 0)
            {
                _messageMonitor.IncrementThrottlingStatistic();

                var stopwatch = Stopwatch.StartNew();

                await _semaphore.WaitAsync().ConfigureAwait(false);
                _semaphore.Release();

                stopwatch.Stop();

                _messageMonitor.HandleThrottlingTime(stopwatch.Elapsed);
            }

            return _semaphore.CurrentCount;
        }

        public Task WaitForThrottlingAsync(bool anyMessagesRetrieved)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">Whether the instance is being disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _semaphore?.Dispose();
                }

                _disposed = true;
            }
        }

        private void EnqueueAction(Func<Task> action)
        {
            try
            {
                _ = Task.Factory.StartNew(async (object semaphore) =>
                {
                    try
                    {
                        await action().ConfigureAwait(false);
                    }
                    finally
                    {
                        ((SemaphoreSlim)semaphore).Release();
                    }
                },
                _semaphore,
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
            }
            catch (Exception)
            {
                _semaphore.Release();
                throw;
            }
        }

        private async Task RunActionAsync(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
