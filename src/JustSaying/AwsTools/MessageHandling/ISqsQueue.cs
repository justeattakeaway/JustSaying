using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface ISqsQueue
    {
        string QueueName { get; }

        string RegionSystemName { get; }

        Uri Uri { get; }

        Task<IList<Message>> GetMessagesAsync(int count, List<string> requestMessageAttributeNames, CancellationToken cancellationToken = default);

        Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request, CancellationToken cancellationToken = default);

        Task<DeleteMessageResponse> DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default);
    }
}
