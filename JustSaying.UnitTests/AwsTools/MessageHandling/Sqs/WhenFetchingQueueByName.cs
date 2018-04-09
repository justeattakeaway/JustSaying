using System.Collections.Generic;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs
{
    public class WhenFetchingQueueByName
    {
        private readonly IAmazonSQS _client;
        private readonly ILoggerFactory _log;
        private const int RetryCount = 3;


        public WhenFetchingQueueByName()
        {
            _client = Substitute.For<IAmazonSQS>();

            _client.GetQueueUrlAsync(Arg.Any<string>())
                .Returns(x =>
                {
                    if (x.Arg<string>() == "some-queue-name")
                        return new GetQueueUrlResponse {QueueUrl = "some-queue-name"};
                    throw new QueueDoesNotExistException("some-queue-name not found");
                });
            _client.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse()
                {
                    Attributes = new Dictionary<string, string> { { "QueueArn", "something:some-queue-name" } }
                });
            _log = Substitute.For<ILoggerFactory>();
        }

        [Fact]
        public void IncorrectQueueNameDoNotMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name1", _client, RetryCount, _log);
            sqsQueueByName.ExistsAsync().GetAwaiter().GetResult().ShouldBeFalse();
        }

        [Fact]
        public void IncorrectPartialQueueNameDoNotMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue", _client, RetryCount, _log);
            sqsQueueByName.ExistsAsync().GetAwaiter().GetResult().ShouldBeFalse();
        }

        [Fact]
        public void CorrectQueueNameShouldMatch()
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name", _client, RetryCount, _log);
            sqsQueueByName.ExistsAsync().GetAwaiter().GetResult().ShouldBeTrue();
        }
    }
}
