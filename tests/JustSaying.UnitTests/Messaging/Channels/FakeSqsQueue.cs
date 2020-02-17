using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using Xunit.Sdk;

namespace JustSaying.UnitTests.Messaging.Channels
{
    public class TestMessage : Message
    {
        public override string ToString()
        {
            return Body;
        }
    }

    public class FakeSqsQueue : ISqsQueue
    {
        public FakeSqsQueue(string queueName, string regionSystemName, Uri uri)
        {
            QueueName = queueName;
            RegionSystemName = regionSystemName;
            Uri = uri;
        }

        public List<Message> SentMessages { get; }

        public string QueueName { get; }
        public string RegionSystemName { get; }
        public Uri Uri { get; }
        public Task<Message[]> GetMessages(int count, List<string> requestMessageAttributeNames, CancellationToken cancellationToken)
        {
            var messages = Enumerable.Range(0, count).Select(x => new TestMessage
            {
                Body = Guid.NewGuid().ToString()
            }).Cast<Message>().ToArray();
            SentMessages.AddRange(messages);
            return Task.FromResult(messages);
        }

        public Task<ReceiveMessageResponse> GetMessages(ReceiveMessageRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteMessageResponse> DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
