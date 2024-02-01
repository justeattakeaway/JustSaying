using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingInBatchAsyncWithGenericMessageSubjectProvider : WhenPublishingTestBase
{
    public class MessageWithTypeParameters<TA, TB> : Message
    {
    }

    private readonly List<Message> _messages = [];
    private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
    private const string TopicArn = "topicarn";

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var topic = new SnsMessagePublisher(TopicArn, Sns, _serializationRegister, NullLoggerFactory.Instance, new GenericMessageSubjectProvider());
        return Task.FromResult(topic);
    }

    protected override void Given()
    {
        for (int i = 0; i < 10; i++)
        {
            var message = new MessageWithTypeParameters<int, string>();
            _messages.Add(message);
            _serializationRegister.Serialize(message, true).Returns("json_message_" + i);
        }

        Sns.FindTopicAsync("TopicName")
            .Returns(new Topic { TopicArn = TopicArn });
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.PublishAsync(_messages);
    }

    [Fact]
    public void MessageIsPublishedToSnsTopic()
    {
        Sns.Received().PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => AssertMessageIsPublishedToSnsTopic(x)));
    }

    private static bool AssertMessageIsPublishedToSnsTopic(PublishBatchRequest request)
    {
        if (request.PublishBatchRequestEntries.Count != 10)
        {
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
           if (request.PublishBatchRequestEntries[i].Message != "json_message_" + i)
           {
               return false;
           }
        }

        return true;
    }

    [Fact]
    public void MessageSubjectIsObjectType()
    {
        string subject = new GenericMessageSubjectProvider().GetSubjectForType(typeof(MessageWithTypeParameters<int, string>));
        Sns.Received().PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => x.PublishBatchRequestEntries.All(y => y.Subject == subject)));
    }

    [Fact]
    public void MessageIsPublishedToCorrectLocation()
    {
        Sns.Received().PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => x.TopicArn == TopicArn));
    }
}
