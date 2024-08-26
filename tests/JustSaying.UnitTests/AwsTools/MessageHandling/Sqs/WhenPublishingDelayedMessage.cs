using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

public class WhenPublishingDelayedMessage : WhenPublishingTestBase
{
    private readonly PublishMessageConverter _publishMessageConverter = new(new NewtonsoftMessageBodySerializer<SimpleMessage>(), new MessageCompressionRegistry([]), new PublishCompressionOptions(), "Subject");
    private const string Url = "https://testurl.com/" + QueueName;

    private readonly SimpleMessage _message = new();
    private readonly PublishMetadata _metadata = new()
    {
        Delay = TimeSpan.FromSeconds(1)
    };

    private const string QueueName = "queuename";

    private protected override Task<SqsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var sqs = new SqsMessagePublisher(new Uri(Url), Sqs, _publishMessageConverter, Substitute.For<ILoggerFactory>());
        return Task.FromResult(sqs);
    }

    protected override void Given()
    {
        Sqs.ListQueuesAsync(Arg.Any<ListQueuesRequest>()).Returns(new ListQueuesResponse { QueueUrls = [Url] });
        Sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.PublishAsync(_message, _metadata);
    }

    [Fact]
    public void MessageIsPublishedWithDelaySecondsPropertySet()
    {
        Sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.DelaySeconds.Equals(1)));
    }
}
