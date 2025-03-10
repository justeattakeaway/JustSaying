using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;
using NSubstitute;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

public class WhenFetchingFifoQueueByName
{
    private readonly IAmazonSQS _client;
    private readonly ILoggerFactory _log;
    private const int RetryCount = 3;

    public WhenFetchingFifoQueueByName()
    {
        _client = Substitute.For<IAmazonSQS>();

        _client.GetQueueUrlAsync(Arg.Any<string>())
            .Returns(x =>
            {
                if (x.Arg<string>() == "some-queue-name.fifo")
                    return new GetQueueUrlResponse
                    {
                        QueueUrl = "https://testqueues.com/some-queue-name.fifo"
                    };
                throw new QueueDoesNotExistException("some-queue-name.fifo not found");
            });
        _client.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
            .Returns(new GetQueueAttributesResponse()
            {
                Attributes = new Dictionary<string, string>
                {
                    { "QueueArn", "something:some-queue-name.fifo" },
                    { "FifoQueue", "true" },
                }
            });
        _log = Substitute.For<ILoggerFactory>();
    }

    [Fact]
    public async Task IncorrectQueueNameDoNotMatch()
    {
        var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name1.fifo", true, _client, RetryCount, _log);
        (await sqsQueueByName.ExistsAsync(CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task IncorrectPartialQueueNameDoNotMatch()
    {
        var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue.fifo", true, _client, RetryCount, _log);
        (await sqsQueueByName.ExistsAsync(CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task CorrectQueueNameShouldMatch()
    {
        var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name.fifo", true, _client, RetryCount, _log);
        (await sqsQueueByName.ExistsAsync(CancellationToken.None)).ShouldBeTrue();
    }
}
