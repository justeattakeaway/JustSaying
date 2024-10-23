using System.Net;
using Amazon.Runtime;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Message = JustSaying.Models.Message;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

public class WhenPublishingInBatchAsyncResponseLoggerAsyncIsCalled : WhenPublishingTestBase
{
    private readonly List<SimpleMessage> _testMessages = new();
    private readonly List<string> _messageIds = new();
    private readonly PublishMessageConverter _publishMessageConverter = new(PublishDestinationType.Queue, new NewtonsoftMessageBodySerializer<SimpleMessage>(), new MessageCompressionRegistry(), new PublishCompressionOptions(), "Subject", false);
    private const string Url = "https://blablabla/" + QueueName;
    private const string QueueName = "queuename";

    private const string RequestId = "TestRequesteId23456";

    private static MessageBatchResponse _response;
    private static IEnumerable<Message> _message = Enumerable.Empty<Message>();

    private protected override Task<SqsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var sqs = new SqsMessagePublisher(new Uri(Url), Sqs, _publishMessageConverter, Substitute.For<ILoggerFactory>())
        {
            MessageBatchResponseLogger = (r, m) =>
            {
                _response = r;
                _message = m;
            }
        };
        return Task.FromResult(sqs);
    }

    protected override void Given()
    {
        for (var i = 0; i < 10; i++)
        {
            _testMessages.Add(new SimpleMessage{ Content = $"Test message {i}" });
            _messageIds.Add("TestMessageId" + i);
        }

        Sqs.GetQueueUrlAsync(Arg.Any<string>())
            .Returns(new GetQueueUrlResponse { QueueUrl = Url });

        Sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
            .Returns(new GetQueueAttributesResponse());

        Sqs.SendMessageBatchAsync(Arg.Any<SendMessageBatchRequest>())
            .Returns(PublishResult);
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.PublishAsync(_testMessages);
    }

    private Task<SendMessageBatchResponse> PublishResult(CallInfo arg)
    {
        var response = new SendMessageBatchResponse
        {
            HttpStatusCode = HttpStatusCode.OK,

            Successful = _messageIds.Select(messageId => new SendMessageBatchResultEntry
            {
                MessageId = messageId
            }).ToList(),

            ResponseMetadata = new ResponseMetadata
            {
                RequestId = RequestId
            }
        };

        return Task.FromResult(response);
    }

    [Fact]
    public void ResponseLoggerIsCalled()
    {
        _response.ShouldNotBeNull();
    }

    [Fact]
    public void ResponseIsForwardedToResponseLogger()
    {
        _response.SuccessfulMessageIds.ShouldBe(_messageIds);
        _response.HttpStatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public void ResponseShouldContainMetadata()
    {
        _response.ResponseMetadata.ShouldNotBeNull();
        _response.ResponseMetadata.RequestId.ShouldNotBeNull();
        _response.ResponseMetadata.RequestId.ShouldBe(RequestId);
    }

    [Fact]
    public void MessageIsForwardedToResponseLogger()
    {
        _message.ShouldNotBeNull();
    }
}
