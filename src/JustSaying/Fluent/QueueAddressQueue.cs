using System;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A queue that is constructed from a Queue URL, that has no behaviour.
    /// </summary>
    internal sealed class QueueAddressQueue : ISqsQueue
    {
        public QueueAddressQueue(Uri queueUri, IAmazonSQS sqsClient)
        {
            Uri = queueUri;
            var hostParts = queueUri.Host.Split('.');
            var pathParts = queueUri.AbsolutePath.Split('/');

            if (pathParts.Length != 2) throw new ArgumentException("Queue Uri did not have a valid path.", nameof(queueUri));
            var region = RegionEndpoint.GetBySystemName(hostParts[1]);
            var accountId = pathParts[0];
            var resource = pathParts[1];
            RegionSystemName = region.SystemName;
            Arn = new Arn { Partition = region.PartitionName, Service = "sqs", Region = region.SystemName, AccountId = accountId, Resource = resource }.ToString();
            Client = sqsClient;
        }

        public InterrogationResult Interrogate()
        {
            return InterrogationResult.Empty;
        }

        public string QueueName { get; }
        public string RegionSystemName { get; }
        public Uri Uri { get; }
        public string Arn { get; }
        public IAmazonSQS Client { get; }
    }
}
