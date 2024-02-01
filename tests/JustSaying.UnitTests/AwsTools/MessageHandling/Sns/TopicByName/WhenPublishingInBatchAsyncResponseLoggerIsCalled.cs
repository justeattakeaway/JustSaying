using System.Net;
using Amazon.Runtime;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.Core;
using Message = JustSaying.Models.Message;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingInBatchAsyncResultLoggerIsCalled : WhenPublishingTestBase
{
    private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
    private readonly List<SimpleMessage> _testMessages = new();
    private readonly List<string> _messageIds = new();
    private const string TopicArn = "topicarn";

    private const string RequestId = "TestRequesteId23456";

    private static MessageBatchResponse _response;
    private static IEnumerable<Message> _messages;

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var topic = new SnsMessagePublisher(TopicArn, Sns, _serializationRegister, NullLoggerFactory.Instance, Substitute.For<IMessageSubjectProvider>())
        {
            MessageBatchResponseLogger = (r, m) =>
            {
                _response = r;
                _messages = m;
            }
        };

        return Task.FromResult(topic);
    }

    protected override void Given()
    {
        for (var i = 0; i < 10; i++)
        {
            _testMessages.Add(new SimpleMessage{ Content = $"Test message {i}" });
            _messageIds.Add("TestMessageId" + i);
        }

        Sns.FindTopicAsync("TopicName")
            .Returns(new Topic { TopicArn = TopicArn });
        Sns.PublishBatchAsync(Arg.Any<PublishBatchRequest>())
            .Returns(PublishResult);
    }

    protected override Task WhenAsync()
    {
        return SystemUnderTest.PublishAsync(new List<Message> { new SimpleMessage() });
    }

    private Task<PublishBatchResponse> PublishResult(CallInfo arg)
    {
        var response = new PublishBatchResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            Successful = _messageIds.Select(messageId => new PublishBatchResultEntry
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
        _messages.ShouldNotBeNull();
    }
}
