using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface ISqsQueue
    {
        IAmazonSQS Client { get; }
        string QueueName { get; }
        RegionEndpoint Region { get; }
        Uri Uri { get; }

        // todo: why aren't these used?/where can they be used externally?
        // Task DeleteAsync();
        // Task<bool> ExistsAsync();
        // Task UpdateQueueAttributeAsync(SqsBasicConfiguration queueConfig);
    }
}
