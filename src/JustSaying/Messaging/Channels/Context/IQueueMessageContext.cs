using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace JustSaying.Messaging.Channels.Context
{
    /// <summary>
    /// A context object that is created by <see cref="Receive.IMessageReceiveBuffer"/> and consumed by
    /// <see cref="Dispatch.IMultiplexerSubscriber"/>
    /// </summary>
    public interface IQueueMessageContext
    {
        /// <summary>
        /// Gets the raw <see cref="Amazon.SQS.Model.Message"/> that is sent over the wire.
        /// </summary>
        Message Message { get; }

        /// <summary>
        /// Updates this messages visibility so that it won't be received by other subscribers before the new timeout expires.
        /// </summary>
        /// <param name="visibilityTimeout">How far into the future to prevent others from receiving this message.</param>
        /// <returns>A <see cref="Task"/> that completes when the update is completed.</returns>
        Task ChangeMessageVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes this message from the SQS queue.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is deleted.</returns>
        Task DeleteMessageFromQueueAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the absolute URI of the SQS queue that this message was received from.
        /// </summary>
        Uri QueueUri { get; }

        /// <summary>
        /// Gets the name of the queue that this message was received from.
        /// </summary>
        string QueueName { get; }
    }
}
