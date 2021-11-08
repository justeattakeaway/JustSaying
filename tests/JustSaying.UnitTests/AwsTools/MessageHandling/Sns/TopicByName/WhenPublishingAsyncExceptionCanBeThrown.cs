using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;
using Xunit;
using Amazon.Runtime;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingAsyncExceptionCanBeThrown : WhenPublishingTestBase
{
    private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
    private const string TopicArn = "topicarn";

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var topic = new SnsMessagePublisher(TopicArn, Sns, _serializationRegister, NullLoggerFactory.Instance, Substitute.For<IMessageSubjectProvider>(), (_, _) => false);
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
    public async Task ExceptionIsThrown()
    {
        await Should.ThrowAsync<PublishException>(() => SystemUnderTest.PublishAsync(new SimpleMessage()));
    }

    [Fact]
    public async Task ExceptionContainsContext()
    {
        try
        {
            await SystemUnderTest.PublishAsync(new SimpleMessage());
        }
        catch (PublishException ex)
        {
            var inner = ex.InnerException as AmazonServiceException;
            inner.ShouldNotBeNull();
            inner.Message.ShouldBe("Operation timed out");
        }
    }

    private static Task<PublishResponse> ThrowsException(CallInfo callInfo)
    {
        throw new AmazonServiceException("Operation timed out");
    }
}