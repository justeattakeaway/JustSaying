using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs
{
    public class WhenPublishingDelayedMessageAsync : XAsyncBehaviourTest<SqsPublisher>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;

        private readonly PublishEnvelope _message = new PublishEnvelope(new SimpleMessage())
        {
            DelaySeconds = 1
        };
        private const string QueueName = "queuename";

        protected override async Task<SqsPublisher> CreateSystemUnderTestAsync()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, _sqs, 0,
                _serialisationRegister, Substitute.For<ILoggerFactory>());
            await sqs.ExistsAsync();
            return sqs;
        }

        protected override Task Given()
        {
            _sqs.ListQueuesAsync(Arg.Any<ListQueuesRequest>()).Returns(new ListQueuesResponse { QueueUrls = new List<string> { Url } });
            _sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(_message, CancellationToken.None);
        }

        [Fact]
        public void MessageIsPublishedWithDelaySecondsPropertySet()
        {
            _sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.DelaySeconds.Equals(1)));
        }
    }
}
