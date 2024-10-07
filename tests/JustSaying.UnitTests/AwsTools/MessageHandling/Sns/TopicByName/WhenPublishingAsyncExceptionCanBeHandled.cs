using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.Core;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingAsyncExceptionCanBeHandled : WhenPublishingTestBase
{
    private const string TopicArn = "topicarn";

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var messageConverter = new PublishMessageConverter(PublishDestinationType.Topic, new NewtonsoftMessageBodySerializer<SimpleMessage>(), new MessageCompressionRegistry(), new PublishCompressionOptions(), "Subject", false);
        var topic = new SnsMessagePublisher(TopicArn, Sns, messageConverter, NullLoggerFactory.Instance, (_, _) => true);

        return Task.FromResult(topic);
    }

    protected override void Given()
    {
        Sns.FindTopicAsync("TopicName")
            .Returns(new Topic { TopicArn = TopicArn });
    }

    protected override Task WhenAsync()
    {
        Sns.PublishAsync(Arg.Any<PublishRequest>()).Returns(ThrowsException);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FailSilently()
    {
        var unexpectedException = await Record.ExceptionAsync(
            () => SystemUnderTest.PublishAsync(new SimpleMessage()));
        unexpectedException.ShouldBeNull();
    }

    private static Task<PublishResponse> ThrowsException(CallInfo callInfo)
    {
        throw new InternalErrorException("Operation timed out");
    }
}
