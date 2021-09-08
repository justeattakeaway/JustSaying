using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class FakeSqsQueue : ISqsQueue
    {
        private readonly Func<CancellationToken, Task<IEnumerable<Message>>> _messageProducer;

        public FakeSqsQueue(Func<CancellationToken, Task<IEnumerable<Message>>> messageProducer, string queueName = "fake-queue-name")
        {
            _messageProducer = messageProducer;
            QueueName = queueName;
            RegionSystemName = "fake-region";
            Uri = new Uri("http://test.com");
            Arn = $"arn:aws:fake-region:123456789012:{queueName}";
        }

        public InterrogationResult Interrogate()
        {
            return InterrogationResult.Empty;
        }

        public string QueueName { get; }
        public string RegionSystemName { get; }
        public Uri Uri { get; set; }
        public string Arn { get; }

        public List<DeleteMessageRequest> DeleteMessageRequests { get; } = new();
        public List<ChangeMessageVisbilityRequest> ChangeMessageVisbilityRequests { get; } = new();
        public List<TagQueueRequest> TagQueueRequests { get; } = new();
        public List<ReceiveMessagesRequest> ReceiveMessageRequests { get; } = new();

        public Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
        {
            DeleteMessageRequests.Add(new DeleteMessageRequest(queueUrl, receiptHandle));
            return Task.CompletedTask;
        }

        public Task TagQueueAsync(string queueUrl, Dictionary<string, string> tags, CancellationToken cancellationToken)
        {
            TagQueueRequests.Add(new TagQueueRequest(queueUrl, tags));
            return Task.CompletedTask;
        }

        public Task<IList<Message>> ReceiveMessagesAsync(string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad, CancellationToken cancellationToken)
        {
            ReceiveMessageRequests.Add(new ReceiveMessagesRequest(queueUrl, maxNumOfMessages, secondsWaitTime, attributesToLoad));
            return Task.FromResult(_messageProducer(cancellationToken) as IList<Message>);
        }

        public Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int visibilityTimeoutInSeconds, CancellationToken cancellationToken)
        {
            ChangeMessageVisbilityRequests.Add(new ChangeMessageVisbilityRequest(queueUrl, receiptHandle, visibilityTimeoutInSeconds));
            return Task.CompletedTask;
        }
    }
}
