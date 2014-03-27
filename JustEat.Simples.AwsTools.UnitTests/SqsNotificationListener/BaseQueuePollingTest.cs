using System;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using AwsTools.UnitTests.MessageStubs;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using System.Collections.Generic;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class BaseQueuePollingTest : BehaviourTest<JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener>
    {
        protected const string QueueUrl = "url";
        protected IAmazonSQS Sqs;
        protected IMessageSerialiser<GenericMessage> Serialiser;
        protected GenericMessage DeserialisedMessage;
        protected const string MessageBody = "object";
        protected IHandler<GenericMessage> Handler;
        protected IMessageMonitor Monitor;
        protected IMessageSerialisationRegister SerialisationRegister;
        protected IMessageFootprintStore MessageFootprintStore;
        private readonly string _messageTypeString = typeof(GenericMessage).ToString();

        protected override JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            
            return new JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener(new SqsQueueByUrl(QueueUrl, Sqs), SerialisationRegister, MessageFootprintStore, Monitor);
        }

        protected override void Given()
        {
            Sqs = Substitute.For<IAmazonSQS>();
            Serialiser = Substitute.For<IMessageSerialiser<GenericMessage>>();
            MessageFootprintStore = Substitute.For<IMessageFootprintStore>();
            SerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            Monitor = Substitute.For<IMessageMonitor>();
            Handler = Substitute.For<IHandler<GenericMessage>>();
            var response = GenerateResponseMessage(_messageTypeString, Guid.NewGuid());
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => response, x => new ReceiveMessageResponse());

            SerialisationRegister.GetSerialiser(_messageTypeString).Returns(Serialiser);
            DeserialisedMessage = new GenericMessage {RaisingComponent = "Component"};
            Serialiser.Deserialise(Arg.Any<string>()).Returns(x => DeserialisedMessage);
        }

        protected override void When()
        {
            SystemUnderTest.AddMessageHandler(Handler);
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