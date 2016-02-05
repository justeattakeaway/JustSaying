using System.Collections.Generic;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.Sqs
{
    class WhenFetchingQueueByName
    {
        private IAmazonSQS _client;
        private const int RetryCount = 3;

        [SetUp]
        protected void SetUp()
        {
            _client = Substitute.For<IAmazonSQS>();
            _client.ListQueues(Arg.Any<ListQueuesRequest>())
                .Returns(new ListQueuesResponse() { QueueUrls = new List<string>() { "some-queue-name" } });
            _client.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse()
                {
                    Attributes = new Dictionary<string, string>() { { "QueueArn", "something:some-queue-name" } }
                });
        }

        [Then]
        public void IncorrectQueueNameDoNotMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name1", _client, RetryCount);
            Assert.IsFalse(sqsQueueByName.Exists());
        }

        [Then]
        public void IncorrectPartialQueueNameDoNotMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue", _client, RetryCount);
            Assert.IsFalse(sqsQueueByName.Exists());
        }

        [Then]
        public void CorrectQueueNameShouldMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name", _client, RetryCount);
            Assert.IsTrue(sqsQueueByName.Exists());
        }
    }
}
