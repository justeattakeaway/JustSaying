using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace JustSaying.Messaging.Channels
{
    internal interface IQueueMessageContext
    {
        Message Message { get; }

        Task ChangeMessageVisibilityAsync(TimeSpan visibilityTimeout);

        Task DeleteMessageFromQueueAsync();

        Uri QueueUri { get; }
    }
}
