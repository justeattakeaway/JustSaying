using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.TopicCreation;

public class WhenFetchingFifoTopicByName
{
    private readonly IAmazonSimpleNotificationService _client;
    private readonly ILoggerFactory _log;

    public WhenFetchingFifoTopicByName()
    {
        _client = Substitute.For<IAmazonSimpleNotificationService>();

        _client.FindTopicAsync(Arg.Any<string>())
            .Returns(x =>
            {
                if (x.Arg<string>() == "some-topic-name.fifo")
                    return new Topic
                    {
                        TopicArn = "something:some-topic-name.fifo"
                    };
                return null;
            });
        _client.GetTopicAttributesAsync(Arg.Any<GetTopicAttributesRequest>())
            .Returns(new GetTopicAttributesResponse()
            {
                Attributes = new Dictionary<string, string>
                {
                    { "TopicArn", "something:some-topic-name.fifo" },
                    { "FifoTopic", "true" },
                }
            });
        _log = Substitute.For<ILoggerFactory>();
    }

    [Fact]
    public async Task IncorrectTopicNameDoNotMatch()
    {
        var snsTopicByName = new SnsTopicByName("some-topic-name1.fifo", true, _client, _log);
        (await snsTopicByName.ExistsAsync(CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task IncorrectPartialTopicNameDoNotMatch()
    {
        var snsTopicByName = new SnsTopicByName("some-topic.fifo", true, _client, _log);
        (await snsTopicByName.ExistsAsync(CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task CorrectQueueNameShouldMatch()
    {
        var snsTopicByName = new SnsTopicByName("some-topic-name.fifo", true, _client, _log);
        (await snsTopicByName.ExistsAsync(CancellationToken.None)).ShouldBeTrue();
    }
}
