using Amazon.Runtime;
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

public class WhenPublishingInBatchAsyncExceptionCanBeThrown : WhenPublishingTestBase
{
    private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
    private const string TopicArn = "topicarn";

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var topic = new SnsMessagePublisher(
            TopicArn,
            Sns,
            _serializationRegister,
            NullLoggerFactory.Instance,
            Substitute.For<IMessageSubjectProvider>())
        {
            HandleBatchException = (_, _) => false,
        };

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
    public async Task ExceptionIsThrown()
    {
        await Should.ThrowAsync<PublishBatchException>(() => SystemUnderTest.PublishAsync(new List<Message> {new SimpleMessage() }));
    }

    [Fact]
    public async Task ExceptionContainsContext()
    {
        try
        {
            await SystemUnderTest.PublishAsync(new List<Message>{ new SimpleMessage()});
        }
        catch (PublishBatchException ex)
        {
            var inner = ex.InnerException as AmazonServiceException;
            inner.ShouldNotBeNull();
            inner.Message.ShouldBe("Operation timed out");
        }
    }

    private static Task<PublishBatchResponse> ThrowsException(CallInfo callInfo)
    {
        throw new AmazonServiceException("Operation timed out");
    }
}
