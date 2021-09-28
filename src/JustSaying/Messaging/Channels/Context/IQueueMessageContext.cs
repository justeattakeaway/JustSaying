using System;
using Amazon.SQS.Model;

namespace JustSaying.Messaging.Channels.Context
{
    /// <summary>
    /// A context object that is created by <see cref="Receive.IMessageReceiveBuffer"/> and consumed by
    /// <see cref="Dispatch.IMultiplexerSubscriber"/>
    /// </summary>
    public interface IQueueMessageContext : IMessageVisibilityUpdater, IMessageDeleter
    {
        /// <summary>
        /// Gets the raw <see cref="Amazon.SQS.Model.Message"/> that is sent over the wire.
        /// </summary>
        Message Message { get; }

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
