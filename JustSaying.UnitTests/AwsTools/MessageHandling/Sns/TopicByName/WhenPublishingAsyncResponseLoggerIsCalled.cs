using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
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
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string TopicArn = "topicarn";

        private const string MessageId = "12345";
        private static MessageResponse _response;
        private static Message _message;
        private readonly IMessageResponseLogger _responseLogger = new NullMessageResponseLogger
        {
            /* Invoke sync version if async version isn't set */
            ResponseLoggerAsync = null,
            ResponseLogger = (r, m) =>
            {
                _response = r;
                _message = m;
            }
        };

        protected override SnsTopicByName CreateSystemUnderTest()
        {
            var topic = new SnsTopicByName("TopicName", _sns, _serialisationRegister, _responseLogger, Substitute.For<ILoggerFactory>(), Substitute.For<SnsWriteConfiguration>());

            topic.Exists();
            return topic;
        }

        protected override void Given()
        {
            _sns.FindTopicAsync("TopicName")
                .Returns(new Topic { TopicArn = TopicArn });
            _sns.PublishAsync(Arg.Any<PublishRequest>())
                .Returns(PublishResult);
        }

        protected override Task When()
        {
            SystemUnderTest.PublishAsync(new GenericMessage()).Wait();

            return Task.CompletedTask;
        }

        private static Task<PublishResponse> PublishResult(CallInfo arg)
        {
            return Task.FromResult(new PublishResponse
            {
                MessageId = MessageId,
                HttpStatusCode = HttpStatusCode.OK
            });
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
        public void MessageIsForwardedToResponseLogger()
        {
            _message.ShouldNotBeNull();
        }
    }
}
