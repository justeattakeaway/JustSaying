using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public class WhenPublishingAsyncResultLoggerIsCalled : XAsyncBehaviourTest<SnsTopicByName>
    {
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string TopicArn = "topicarn";

        private const string MessageId = "TestMessage12345";
        private const string RequestId = "TestRequesteId23456";

        private static MessageResponse _response;
        private static Message _message;

        protected override async Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            var topic = new SnsTopicByName("TopicName", _sns, _serializationRegister, Substitute.For<ILoggerFactory>(), Substitute.For<SnsWriteConfiguration>(), Substitute.For<IMessageSubjectProvider>())
            {
                MessageResponseLogger = (r, m) =>
                {
                    _response = r;
                    _message = m;
                }
            };

            await topic.ExistsAsync();
            return topic;
        }

        protected override Task Given()
        {
            _sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
            _sns.PublishAsync(Arg.Any<PublishRequest>())
                .Returns(PublishResult);

            return Task.CompletedTask;
        }

        protected override Task When()
        {
            return SystemUnderTest.PublishAsync(new SimpleMessage());
        }

        private static Task<PublishResponse> PublishResult(CallInfo arg)
        {
            var response = new PublishResponse
            {
                MessageId = MessageId,
                HttpStatusCode = HttpStatusCode.OK,
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
            _response.MessageId.ShouldBe(MessageId);
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
}
