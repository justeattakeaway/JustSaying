using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Xunit;
using Shouldly;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenThereAreExceptionsInMessageProcessing : XAsyncBehaviourTest<AwsTools.MessageHandling.SqsNotificationListener>
    {
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private readonly IMessageSerialisationRegister _serialisationRegister = 
            Substitute.For<IMessageSerialisationRegister>();
        
        private int _callCount;

        protected override AwsTools.MessageHandling.SqsNotificationListener CreateSystemUnderTest()
        {
            return new AwsTools.MessageHandling.SqsNotificationListener(
                new SqsQueueByUrl(RegionEndpoint.EUWest1, "", _sqs), 
                _serialisationRegister, 
                Substitute.For<IMessageMonitor>(),
                Substitute.For<ILoggerFactory>());
        }

        protected override void Given()
        {
            _serialisationRegister
                .DeserializeMessage(Arg.Any<string>())
                .Returns(x => { throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing"); });
            _sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(GenerateEmptyMessage()));

            _sqs.When(x => x.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>()))
                .Do(x => _callCount++);
        }

        protected override async Task When()
        {
            SystemUnderTest.Listen();
            await Task.Delay(100);
            SystemUnderTest.StopListening();
            await Task.Yield();
        }

        [Fact]
        public void TheListenerDoesNotDie()
        {
            _callCount.ShouldBeGreaterThanOrEqualTo(3);
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse
            {
                Messages = new List<Message>()
            };
        }
    }
}
