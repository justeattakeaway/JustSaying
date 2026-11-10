using System.Net;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenApplyingMaximumMessageSize : WhenSnsTopicTestBase
{
    private const string TopicArn = "topicarn";

    private SetTopicAttributesRequest _actualRequest;

    private protected override Task<SnsTopicByName> CreateSystemUnderTestAsync()
    {
        var topicByName = new SnsTopicByName("TopicName", Sns, NullLoggerFactory.Instance)
        {
            MaximumMessageSize = JustSayingConstants.MaximumSnsMessageSize
        };

        return Task.FromResult(topicByName);
    }

    protected override void Given()
    {
        Sns.FindTopicAsync(Arg.Any<string>())
            .Returns(new Topic { TopicArn = TopicArn });

        // The attribute is absent until it has been explicitly set, meaning the SNS default.
        Sns.GetTopicAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GetTopicAttributesResponse { Attributes = [] });

        Sns.SetTopicAttributesAsync(Arg.Any<SetTopicAttributesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SetTopicAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        Sns.When(x => x.SetTopicAttributesAsync(Arg.Any<SetTopicAttributesRequest>(), Arg.Any<CancellationToken>()))
            .Do(x => _actualRequest = x.Arg<SetTopicAttributesRequest>());
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.ExistsAsync(CancellationToken.None);
        await SystemUnderTest.ApplyMaximumMessageSizeAsync(CancellationToken.None);
    }

    [Test]
    public void TheAttributeIsSetOnTheTopic()
    {
        _actualRequest.ShouldNotBeNull();
        _actualRequest.TopicArn.ShouldBe(TopicArn);
        _actualRequest.AttributeName.ShouldBe(JustSayingConstants.AttributeMaximumMessageSize);
        _actualRequest.AttributeValue.ShouldBe("1048576");
    }
}

public class WhenMaximumMessageSizeIsNotConfigured : WhenSnsTopicTestBase
{
    private protected override Task<SnsTopicByName> CreateSystemUnderTestAsync()
        => Task.FromResult(new SnsTopicByName("TopicName", Sns, NullLoggerFactory.Instance));

    protected override void Given()
    {
        Sns.FindTopicAsync(Arg.Any<string>())
            .Returns(new Topic { TopicArn = "topicarn" });
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.ExistsAsync(CancellationToken.None);
        await SystemUnderTest.ApplyMaximumMessageSizeAsync(CancellationToken.None);
    }

    [Test]
    public void TheTopicAttributesAreLeftAlone()
    {
        Sns.DidNotReceive().SetTopicAttributesAsync(Arg.Any<SetTopicAttributesRequest>(), Arg.Any<CancellationToken>());
        Sns.DidNotReceive().GetTopicAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

public class WhenMaximumMessageSizeAlreadyMatches : WhenSnsTopicTestBase
{
    private protected override Task<SnsTopicByName> CreateSystemUnderTestAsync()
        => Task.FromResult(new SnsTopicByName("TopicName", Sns, NullLoggerFactory.Instance)
        {
            MaximumMessageSize = JustSayingConstants.DefaultSnsMaximumMessageSize
        });

    protected override void Given()
    {
        Sns.FindTopicAsync(Arg.Any<string>())
            .Returns(new Topic { TopicArn = "topicarn" });

        Sns.GetTopicAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GetTopicAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    [JustSayingConstants.AttributeMaximumMessageSize] = "262144"
                }
            });
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.ExistsAsync(CancellationToken.None);
        await SystemUnderTest.ApplyMaximumMessageSizeAsync(CancellationToken.None);
    }

    [Test]
    public void NoAttributeUpdateIsIssued()
    {
        Sns.DidNotReceive().SetTopicAttributesAsync(Arg.Any<SetTopicAttributesRequest>(), Arg.Any<CancellationToken>());
    }
}
