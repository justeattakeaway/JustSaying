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

        private bool _disposed;

        public Throttled(
            int maxWorkers,
            TimeSpan startTimeout,
            IMessageMonitor messageMonitor,
            ILogger logger)
        {
            if (maxWorkers < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWorkers), maxWorkers, "At least one worker must be specified.");
            }

            if (startTimeout <= TimeSpan.Zero && startTimeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(startTimeout), startTimeout, "The start timeout must be a positive value.");
            }

            _messageMonitor = messageMonitor ?? throw new ArgumentNullException(nameof(messageMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            MaxWorkers = maxWorkers;
            StartTimeout = startTimeout;

            _semaphore = new SemaphoreSlim(MaxWorkers, MaxWorkers);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Throttled"/> class.
        /// </summary>
        ~Throttled()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public int AvailableWorkers => _semaphore.CurrentCount;

        /// <inheritdoc />
        public int MaxWorkers { get; }

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
    }
}
