using System.Collections.Generic;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.Sqs
{
    class WhenFetchingQueueByName
    {
        private IAmazonSQS _client;
        private ILoggerFactory _log;
        private const int RetryCount = 3;

        [SetUp]
        protected void SetUp()
        {
            _client = Substitute.For<IAmazonSQS>();
            _client.ListQueuesAsync(Arg.Any<ListQueuesRequest>())
                .Returns(new ListQueuesResponse { QueueUrls = new List<string> { "some-queue-name" } });
            _client.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse
                {
                    Attributes = new Dictionary<string, string> { { "QueueArn", "something:some-queue-name" } }
                });
            _log = Substitute.For<ILoggerFactory>();
        }

        [Then]
        public void IncorrectQueueNameDoNotMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name1", _client, RetryCount, _log);
            Assert.IsFalse(sqsQueueByName.ExistsAsync().GetAwaiter().GetResult());
        }

        [Then]
        public void IncorrectPartialQueueNameDoNotMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue", _client, RetryCount, _log);
            Assert.IsFalse(sqsQueueByName.ExistsAsync().GetAwaiter().GetResult());
        }

        [Then]
        public void CorrectQueueNameShouldMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name", _client, RetryCount, _log);
            Assert.IsTrue(sqsQueueByName.ExistsAsync().GetAwaiter().GetResult());
        }
    }
}
