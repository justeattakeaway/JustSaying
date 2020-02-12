using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface ISqsQueue
    {
        string QueueName { get; }

        string RegionSystemName { get; }

        Uri Uri { get; }

        Task<IList<Message>> GetMessages(int count, List<string> requestMessageAttributeNames, CancellationToken cancellationToken);

        Task<ReceiveMessageResponse> GetMessages(ReceiveMessageRequest request, CancellationToken cancellationToken);

        Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request, CancellationToken cancellationToken = default);

        Task<DeleteMessageResponse> DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default);

        IQueueMessageContext CreateQueueMessageContext(Message message);
    }
}
