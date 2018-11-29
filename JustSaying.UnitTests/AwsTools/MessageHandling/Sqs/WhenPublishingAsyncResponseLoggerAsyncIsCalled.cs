using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.Messaging;
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

        private const string MessageId = "TestMessage12345";
        private const string RequestId = "TestRequesteId23456";

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
            var response = new SendMessageResponse
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
