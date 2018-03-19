using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;
using Xunit;
using Message = JustSaying.Models.Message;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs
{
    public class WhenPublishingAsyncResponseLoggerAsyncIsCalled : XAsyncBehaviourTest<SqsPublisher>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly GenericMessage _testMessage = new GenericMessage {Content = "Hello"};
        private const string QueueName = "queuename";

        private const string MessageId = "12345";
        private static MessageResponse _response;
        private static Message _message;
        public readonly IMessageResponseLogger _responseLogger = new DefaultMessageResponseLogger
        {
            ResponseLoggerAsync = (r, m) =>
            {
                _response = r;
                _message = m;
                return Task.CompletedTask;
            }
        };

        protected override SqsPublisher CreateSystemUnderTest()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, _sqs, 0, _serialisationRegister, _responseLogger, Substitute.For<ILoggerFactory>());
            sqs.Exists();
            return sqs;
        }

        protected override void Given()
        {
            _sqs.GetQueueUrlAsync(Arg.Any<string>())
                .Returns(new GetQueueUrlResponse { QueueUrl = Url });

            _sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse());

            _serialisationRegister.Serialise(_testMessage, false)
                .Returns("serialized_contents");

            _sqs.SendMessageAsync(Arg.Any<SendMessageRequest>())
                .Returns(PublishResult);
        }

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(_testMessage);
        }

        private static Task<SendMessageResponse> PublishResult(CallInfo arg)
        {
            return Task.FromResult(new SendMessageResponse
            {
                MessageId = MessageId,
                HttpStatusCode = HttpStatusCode.OK
            });
        }

        [Fact]
        public void ResponseLoggerAsyncIsCalled()
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
