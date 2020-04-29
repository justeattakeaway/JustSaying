using System;
using System.Collections.Generic;
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
        Message Message { get; }

        MessageAttributes MessageAttributes { get; }

        Task ChangeMessageVisibilityAsync(TimeSpan visibilityTimeout);

        Task DeleteMessageFromQueueAsync();

        Uri QueueUri { get; }

        string QueueName { get; }
    }
}
