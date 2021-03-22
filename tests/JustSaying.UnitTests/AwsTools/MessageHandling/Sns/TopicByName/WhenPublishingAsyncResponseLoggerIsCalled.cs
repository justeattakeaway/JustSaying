using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SimpleNotificationService.Model;
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
#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public class WhenPublishingAsyncResultLoggerIsCalled : WhenPublishingTestBase
    {
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private const string TopicArn = "topicarn";

        private const string MessageId = "TestMessage12345";
        private const string RequestId = "TestRequesteId23456";

        private static MessageResponse _response;
        private static Message _message;

        private protected override async Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            var topic = new SnsTopicByName("TopicName", Sns, _serializationRegister, Substitute.For<ILoggerFactory>(), Substitute.For<SnsWriteConfiguration>(), Substitute.For<IMessageSubjectProvider>())
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

        protected override void Given()
        {
            Sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
            Sns.PublishAsync(Arg.Any<PublishRequest>())
                .Returns(PublishResult);
        }

        protected override Task WhenAsync()
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
