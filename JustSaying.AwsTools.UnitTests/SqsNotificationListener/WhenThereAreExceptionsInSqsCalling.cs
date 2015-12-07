using System;
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
        private readonly string _messageTypeString = typeof(GenericMessage).ToString();
        protected override void Given()
        {
            Sqs = Substitute.For<ISqsClient>();
            Serialiser = Substitute.For<IMessageSerialiser>();
            SerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            Monitor = Substitute.For<IMessageMonitor>();
            Handler = Substitute.For<IHandler<GenericMessage>>();
            GenerateResponseMessage(_messageTypeString, Guid.NewGuid());

            SerialisationRegister
                .GeTypeSerialiser(_messageTypeString)
                .Returns(new TypeSerialiser(typeof(GenericMessage), Serialiser));

            DeserialisedMessage = new GenericMessage { RaisingComponent = "Component" };
            Serialiser
                .Deserialise(Arg.Any<string>(), typeof(GenericMessage))
                .Returns(x => DeserialisedMessage);

            Sqs.When(x => x.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()))
                .Do(_ =>
                {
                    _sqsCallCounter++;
                    throw new Exception();
                });
            Sqs.When(x => x.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>()))
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