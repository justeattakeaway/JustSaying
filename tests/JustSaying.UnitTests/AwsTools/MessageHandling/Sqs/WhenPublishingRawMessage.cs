using System.Text.Json.Nodes;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

public class WhenPublishingRawMessage : WhenPublishingTestBase
{
    private readonly PublishMessageConverter _publishMessageConverter = CreateConverter(isRawMessage: true);
    private const string Url = "https://blablabla/" + QueueName;
    private readonly SimpleMessage _message = new() { Content = "Hello" };
    private const string QueueName = "queuename";
    private string _capturedMessageBody;

    private protected override Task<SqsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var sqs = new SqsMessagePublisher(new Uri(Url), Sqs, _publishMessageConverter, Substitute.For<ILoggerFactory>());
        return Task.FromResult(sqs);
    }

    protected override void Given()
    {
        Sqs.GetQueueUrlAsync(Arg.Any<string>())
            .Returns(new GetQueueUrlResponse { QueueUrl = Url });

        Sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
            .Returns(new GetQueueAttributesResponse());

        Sqs.SendMessageAsync(Arg.Do<SendMessageRequest>(x => _capturedMessageBody = x.MessageBody));
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.PublishAsync(_message);
    }

    [Fact]
    public void MessageIsPublishedToQueue()
    {
        _capturedMessageBody.ShouldNotBeNull();
        var jsonNode = JsonNode.Parse(_capturedMessageBody).ShouldNotBeNull();
        var content = jsonNode["Content"].ShouldNotBeNull().GetValue<string>();
        content.ShouldBe("Hello");
    }

    [Fact]
    public void MessageIsPublishedToCorrectLocation()
    {
        Sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.QueueUrl == Url));
    }
}
