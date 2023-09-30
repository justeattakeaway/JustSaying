using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingAsync : WhenPublishingTestBase
{
    private const string Message = "the_message_in_json";
    private const string MessageAttributeKey = "StringAttribute";
    private const string MessageAttributeValue = "StringValue";
    private const string MessageAttributeDataType = "String";
    private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
    private const string TopicArn = "topicarn";

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var topic = new SnsMessagePublisher(TopicArn, Sns, _serializationRegister, NullLoggerFactory.Instance, new NonGenericMessageSubjectProvider());
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
        var metadata = new PublishMetadata()
            .AddMessageAttribute(MessageAttributeKey, MessageAttributeValue);

        await SystemUnderTest.PublishAsync(new SimpleMessage(), metadata);
    }

    [Fact]
    public void MessageIsPublishedToSnsTopic()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => B(x)));
    }

    private static bool B(PublishRequest x)
    {
        return x.Message.Equals(Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public void MessageSubjectIsObjectType()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.Subject == typeof(SimpleMessage).Name));
    }

    [Fact]
    public void MessageIsPublishedToCorrectLocation()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.TopicArn == TopicArn));
    }

    [Fact]
    public void MessageAttributeKeyIsPublished()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.MessageAttributes.Single().Key == MessageAttributeKey));
    }

    [Fact]
    public void MessageAttributeValueIsPublished()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.MessageAttributes.Single().Value.StringValue == MessageAttributeValue));
    }

    [Fact]
    public void MessageAttributeDataTypeIsPublished()
    {
        Sns.Received().PublishAsync(Arg.Is<PublishRequest>(x => x.MessageAttributes.Single().Value.DataType == MessageAttributeDataType));
    }
}
