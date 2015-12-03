using System;
using Amazon;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;
using JustSaying.Messaging.MessageSerialisation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public abstract class BaseQueuePollingTest : BehaviourTest<JustSaying.AwsTools.SqsNotificationListener>
    {
        protected const string QueueUrl = "url";
        protected ISqsClient Sqs;
        protected IMessageSerialiser Serialiser;
        protected GenericMessage DeserialisedMessage;
        protected const string MessageBody = "object";
        protected IHandler<GenericMessage> Handler;
        protected IMessageMonitor Monitor;
        protected IMessageSerialisationRegister SerialisationRegister;
        protected IMessageLock MessageLock;
        private readonly string _messageTypeString = typeof(GenericMessage).ToString();

        protected override JustSaying.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustSaying.AwsTools.SqsNotificationListener(new SqsQueueByUrl(QueueUrl, Sqs), SerialisationRegister, Monitor, null, MessageLock);
        }

        protected override void Given()
        {
            Sqs = Substitute.For<ISqsClient>();
            Serialiser = Substitute.For<IMessageSerialiser>();
            SerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            Monitor = Substitute.For<IMessageMonitor>();
            Handler = Substitute.For<IHandler<GenericMessage>>();

            var response = GenerateResponseMessage(_messageTypeString, Guid.NewGuid());

            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>())
               .Returns(
                    x => response, 
                    x => new ReceiveMessageResponse());
            Sqs.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>())
                .Returns(
                    x => Task.FromResult(response),
                    x => Task.FromResult(new ReceiveMessageResponse()));
            Sqs.Region.Returns(RegionEndpoint.EUWest1);

            SerialisationRegister.GeTypeSerialiser(_messageTypeString).Returns(new TypeSerialiser(typeof(GenericMessage), Serialiser));
            DeserialisedMessage = new GenericMessage {RaisingComponent = "Component"};
            Serialiser.Deserialise(Arg.Any<string>(), typeof(GenericMessage)).Returns(x => DeserialisedMessage);
        }

        protected override void When()
        {
            SystemUnderTest.AddMessageHandler(() => Handler);
            SystemUnderTest.Listen();
        }

        protected ReceiveMessageResponse GenerateResponseMessage(string messageType, Guid messageId)
        {
            return new ReceiveMessageResponse
            {
                Messages = new List<Message> {       
                    new Message
                    {   
                        MessageId = messageId.ToString(),
                        Body = "{\"Subject\":\"" + messageType + "\"," + "\"Message\":\"" + MessageBody + "\"}"
                    },
                    new Message
                    {
                        MessageId = messageId.ToString(),
                        Body = "{\"Subject\":\"SOME_UNKNOWN_MESSAGE\"," + "\"Message\":\"SOME_RANDOM_MESSAGE\"}"
                    }}
            };
        }

        public override void PostAssertTeardown()
        {
            SystemUnderTest.StopListening();
        }
    }
}