using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface ISqsQueue : IInterrogable
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
