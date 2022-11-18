using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.Core;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingInBatchAsyncExceptionCanBeHandled : WhenPublishingTestBase
{
    private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
    private const string TopicArn = "topicarn";

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var topic = new SnsMessagePublisher(TopicArn, Sns, _serializationRegister, NullLoggerFactory.Instance, Substitute.For<IMessageSubjectProvider>(), null, (_, _) => true);

        return Task.FromResult(topic);
    }

    protected override void Given()
    {
        Sns.FindTopicAsync("TopicName")
            .Returns(new Topic { TopicArn = TopicArn });
    }

    protected override Task WhenAsync()
    {
        Sns.PublishBatchAsync(Arg.Any<PublishBatchRequest>()).Returns(ThrowsException);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FailSilently()
    {
        var unexpectedException = await Record.ExceptionAsync(
            () => SystemUnderTest.PublishAsync(new List<Message> { new SimpleMessage() }));
        unexpectedException.ShouldBeNull();
    }

    private static Task<PublishBatchResponse> ThrowsException(CallInfo callInfo)
    {
        throw new InternalErrorException("Operation timed out");
    }
}
