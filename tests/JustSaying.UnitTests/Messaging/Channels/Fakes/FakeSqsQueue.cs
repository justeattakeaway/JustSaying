using System;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class FakeSqsQueue : ISqsQueue
    {
        public FakeSqsQueue(string queueName, IAmazonSQS client)
        {
            QueueName = queueName;
            RegionSystemName = "fake-region";
            Uri = new Uri("http://test.com");
            Client = client;
        }

        public InterrogationResult Interrogate()
        {
            return InterrogationResult.Empty;
        }

        public string QueueName { get; }
        public string RegionSystemName { get; }
        public Uri Uri { get; }
        public IAmazonSQS Client { get; }

        public FakeAmazonSqs FakeClient => Client as FakeAmazonSqs;
    }
}
