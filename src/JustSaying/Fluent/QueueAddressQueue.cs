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
        public QueueAddressQueue(QueueAddress queueAddress, IAmazonSQS sqsClient)
        {
            Uri = queueAddress.QueueUrl;
            var pathParts = queueAddress.QueueUrl.AbsolutePath.TrimStart('/').Split('/');

            var region = RegionEndpoint.GetBySystemName(queueAddress.RegionName);
            var accountId = pathParts[0];
            var resource = pathParts[1];
            RegionSystemName = region.SystemName;
            QueueName = resource;
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
