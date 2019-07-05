using System;
using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    /// <summary>
    /// Defines a strategy for processing messages asynchronously.
    /// </summary>
    public interface IMessageProcessingStrategy
    {
        /// <summary>
        /// Gets the maximum number of messages that can be processed concurrently.
        /// </summary>
        int MaxConcurrency { get; }

        /// <summary>
        /// Starts a worker task to process a message as an asynchronous operation.
        /// </summary>
        /// <param name="action">A delegate to a method that processes the message.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation of queueing <paramref name="action"/>,
        /// including waiting for a worker to become available. If the task returns <see cref="false"/>
        /// an available worker was not available and the action was not queued to be run.
        /// </returns>
        Task<bool> StartWorkerAsync(Func<Task> action, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to wait for a available worker as an asynchronous operation.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation to wait for an
        /// available worker which returns the current number of available workers.
        /// </returns>
        /// <remarks>
        /// The task returned by this method completing does not guarantee that a worker
        /// will be available immediately when <see cref="StartWorkerAsync"/> is subsequently called.
        /// </remarks>
        Task<int> WaitForAvailableWorkerAsync();
    }
}
