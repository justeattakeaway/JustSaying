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

        Task<IList<Message>> GetMessagesAsync(int count, IEnumerable<string> requestMessageAttributeNames,
            CancellationToken cancellationToken = default);

        Task ChangeMessageVisibilityAsync(string receiptHandle, int timeoutInSeconds, CancellationToken cancellationToken = default);

        Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default);
    }
}
