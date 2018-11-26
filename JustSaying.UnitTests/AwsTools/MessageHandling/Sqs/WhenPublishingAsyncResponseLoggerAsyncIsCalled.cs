using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
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
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly SimpleMessage _testMessage = new SimpleMessage { Content = "Hello" };
        private const string QueueName = "queuename";

        private const string MessageId = "12345";
        private static MessageResponse _response;
        private static Message _message;

        protected override async Task<SqsPublisher> CreateSystemUnderTestAsync()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, _sqs, 0, _serializationRegister, Substitute.For<ILoggerFactory>())
            {
                MessageResponseLogger = (r, m) =>
                {
                    _response = r;
                    _message = m;
                }
            };
            await sqs.ExistsAsync();
            return sqs;
        }

        protected override Task Given()
        {
            _sqs.GetQueueUrlAsync(Arg.Any<string>())
                .Returns(new GetQueueUrlResponse { QueueUrl = Url });

            _sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse());

            _serializationRegister.Serialize(_testMessage, false)
                .Returns("serialized_contents");

            _sqs.SendMessageAsync(Arg.Any<SendMessageRequest>())
                .Returns(PublishResult);

            return Task.CompletedTask;
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
