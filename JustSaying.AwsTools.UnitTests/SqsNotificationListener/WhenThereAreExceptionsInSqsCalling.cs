using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustBehave;
using JustSaying.AwsTools;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenThereAreExceptionsInSqsCalling : BaseQueuePollingTest
    {
        private int _sqsCallCounter;
        protected override void Given()
        {
            Sqs = Substitute.For<ISqsClient>();
            SerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            Monitor = Substitute.For<IMessageMonitor>();
            Handler = Substitute.For<IHandler<GenericMessage>>();
            GenerateResponseMessage(_messageTypeString, Guid.NewGuid());

            DeserialisedMessage = new GenericMessage { RaisingComponent = "Component" };
            
            Sqs.When(x => x.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>()))
                .Do(_ =>
                {
                    _sqsCallCounter++;
                    throw new Exception();
                });
            Sqs.Region.Returns(RegionEndpoint.EUWest1);
        }

        protected override void When()
        {
            SystemUnderTest.AddMessageHandler(() => Handler);
            SystemUnderTest.Listen();
        }

        [Then]
        public async Task QueueIsPolledMoreThanOnce()
        {
            await Patiently.AssertThatAsync(() => _sqsCallCounter > 1);
        }

        public override void PostAssertTeardown()
        {
            SystemUnderTest.StopListening();
            base.PostAssertTeardown();
        }
    }
}