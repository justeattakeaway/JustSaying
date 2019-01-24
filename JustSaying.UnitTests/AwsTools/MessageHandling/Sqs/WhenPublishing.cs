using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs
{
    public class WhenPublishing : IAsyncLifetime
    {
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly SimpleMessage _message = new SimpleMessage { Content = "Hello" };
        private const string QueueName = "queuename";

        private SqsPublisher _systemUnderTest;

        public virtual async Task InitializeAsync()
        {
            Given();

            _systemUnderTest = await CreateSystemUnderTestAsync();

            await When().ConfigureAwait(false);
        }

        public virtual Task DisposeAsync()
        {
            if (_sqs != null)
            {
                _sqs.Dispose();
                _sqs = null;
            }

            return Task.CompletedTask;
        }

        private async Task<SqsPublisher> CreateSystemUnderTestAsync()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, _sqs, 0, _serializationRegister, Substitute.For<ILoggerFactory>());
            await sqs.ExistsAsync();
            return sqs;
        }

        private void Given()
        {
            _sqs.GetQueueUrlAsync(Arg.Any<string>())
                .Returns(new GetQueueUrlResponse {QueueUrl = Url});

            _sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse());

            _serializationRegister.Serialize(_message, false)
                .Returns("serialized_contents");
        }

        private async Task When()
        {
            await _systemUnderTest.PublishAsync(_message);
        }

        [Fact]
        public void MessageIsPublishedToQueue()
        {
            // ToDo: Could be better...
            _sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(
                x => x.MessageBody.Equals("serialized_contents", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.QueueUrl == Url));
        }
    }
}
