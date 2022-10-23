using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingAsyncWithGenericMessageSubjectProvider : WhenPublishingTestBase
{
    public class MessageWithTypeParameters<TA, TB> : Message
    {
    }

    private const string Message = "the_message_in_json";
    private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
    private const string TopicArn = "topicarn";

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var topic = new SnsMessagePublisher(TopicArn, Sns, _serializationRegister, NullLoggerFactory.Instance, new GenericMessageSubjectProvider());
        return Task.FromResult(topic);
    }

    protected override void Given()
    {
        _serializationRegister.Serialize(Arg.Any<Message>(), Arg.Is(true)).Returns(Message);
        Sns.FindTopicAsync("TopicName")
            .Returns(new Topic { TopicArn = TopicArn });
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.PublishAsync(new MessageWithTypeParameters<int, string>());
    }

    [Fact]
    public void MessageIsPublishedToSnsTopic()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => B(x)));
    }

    private static bool B(PublishRequest x)
    {
        return x.Message.Equals(Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MessageSubjectIsObjectType()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.Subject == new GenericMessageSubjectProvider().GetSubjectForType(typeof(MessageWithTypeParameters<int, string>))));
    }

    [Fact]
    public void MessageIsPublishedToCorrectLocation()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.TopicArn == TopicArn));
    }
}