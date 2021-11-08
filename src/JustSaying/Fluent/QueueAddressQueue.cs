using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Extensions;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A queue that is constructed from a Queue URL, that has no behaviour.
    /// </summary>
    internal sealed class QueueAddressQueue : ISqsQueue
    {
        private readonly IAmazonSQS _client;

        public QueueAddressQueue(QueueAddress queueAddress, IAmazonSQS sqsClient)
        {
            _client = sqsClient;

            Uri = queueAddress.QueueUrl;
            var pathSegments = queueAddress.QueueUrl.Segments.Select(x => x.Trim('/')).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (pathSegments.Length != 2) throw new ArgumentException("Queue Url was not correctly formatted. Path should contain 2 segments.");

            var region = RegionEndpoint.GetBySystemName(queueAddress.RegionName);
            var accountId = pathSegments[0];
            var resource = pathSegments[1];
            RegionSystemName = region.SystemName;
            QueueName = resource;
            Arn = new Arn { Partition = region.PartitionName, Service = "sqs", Region = region.SystemName, AccountId = accountId, Resource = resource }.ToString();
        }

        public InterrogationResult Interrogate()
        {
            return InterrogationResult.Empty;
        }

        public string QueueName { get; }
        public string RegionSystemName { get; }
        public Uri Uri { get; }
        public string Arn { get; }

        public Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
            => _client.DeleteMessageAsync(queueUrl, receiptHandle, cancellationToken);

        public Task TagQueueAsync(string queueUrl, Dictionary<string, string> tags, CancellationToken cancellationToken)
            => _client.TagQueueAsync(queueUrl, tags, cancellationToken);

        public Task<IList<Message>> ReceiveMessagesAsync(string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad, CancellationToken cancellationToken)
            => _client.ReceiveMessagesAsync(queueUrl, maxNumOfMessages, secondsWaitTime, attributesToLoad, cancellationToken);

        public Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int visibilityTimeoutInSeconds, CancellationToken cancellationToken)
            => _client.ChangeMessageVisibilityAsync(queueUrl, receiptHandle, visibilityTimeoutInSeconds, cancellationToken);

    }
}
