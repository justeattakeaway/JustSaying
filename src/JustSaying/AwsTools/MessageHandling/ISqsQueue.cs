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

        Task<IList<Message>> GetMessagesAsync(int maximumCount, IEnumerable<string> requestMessageAttributeNames,
            CancellationToken stoppingToken = default);

        Task ChangeMessageVisibilityAsync(string receiptHandle, TimeSpan timeout, CancellationToken cancellationToken = default);

        Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default);
    }
}
