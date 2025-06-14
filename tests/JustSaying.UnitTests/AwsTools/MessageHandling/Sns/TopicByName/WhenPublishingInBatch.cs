using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Models;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingInBatch : WhenPublishingTestBase
{
    private const string Message = "the_message_in_json";
    private const string TopicArn = "topicarn";

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var messageConverter = CreateConverter(new FakeBodySerializer(Message));
        var topic = new SnsMessagePublisher(TopicArn, Sns, messageConverter, NullLoggerFactory.Instance, null, null);
        return Task.FromResult(topic);
    }

    protected override void Given()
    {
        Sns.FindTopicAsync("TopicName")
            .Returns(new Topic { TopicArn = TopicArn });
    }

    protected override async Task WhenAsync()
    {
        var messages = new List<Message>();
        for (int i = 0; i < 1_000; i++)
        {
            messages.Add(new SimpleMessage
            {
                Content = $"Message {i}"
            });
        }

        await SystemUnderTest.PublishAsync(messages);
    }

    [Fact]
    public void MultipleMessageIsPublishedToSnsTopic()
    {
        Sns.Received(100).PublishBatchAsync(Arg.Any<PublishBatchRequest>());
    }

    [Fact]
    public void MessageIsPublishedToSnsTopic()
    {
        Sns.Received().PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => AssertMessageIsPublishedToSnsTopic(x)));
    }

    private static bool AssertMessageIsPublishedToSnsTopic(PublishBatchRequest request)
    {
        if (!request.PublishBatchRequestEntries.Count.Equals(10))
        {
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            var entry = request.PublishBatchRequestEntries[i];
            if (entry.Message.Equals($"Message {i}"))
            {
                return false;
            }
        }

        return true;
    }

    [Fact]
    public void MessageSubjectIsObjectType()
    {
        Sns.Received().PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => AssertMessageSubjectIsObjectType(x)));
    }

    private static bool AssertMessageSubjectIsObjectType(PublishBatchRequest request)
    {
        for (int i = 0; i < 10; i++)
        {
            var entry = request.PublishBatchRequestEntries[i];
            if (!entry.Subject.Equals(nameof(SimpleMessage)))
            {
                return false;
            }
        }

        return true;
    }

    [Fact]
    public void MessageIsPublishedToCorrectLocation()
    {
        Sns.Received().PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => x.TopicArn == TopicArn));
    }
}
