using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace JustSaying.Messaging.Channels
{
    public interface IQueueMessageContext
    {
        Message Message { get; }

        Task ChangeMessageVisibilityAsync(int visibilityTimeoutSeconds);

        Task DeleteMessageFromQueue();

        Uri QueueUri { get; }
    }
}