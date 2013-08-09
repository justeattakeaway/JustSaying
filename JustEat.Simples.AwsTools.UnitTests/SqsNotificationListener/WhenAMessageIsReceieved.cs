using System;
using System.Threading;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;
using JustEat.Simples.NotificationStack.Messaging.Messages.Sms;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenAMessageIsReceieved : BaseQueuePollingTest
    {
        private Guid _messageId;

        protected override void Given()
        {
            _messageId = Guid.NewGuid();
            DeserialisedMessage = new CustomerOrderRejectionSms(1, 2, "3", SmsCommunicationActivity.ConfirmedReceived) { Id = _messageId };
            Serialiser.Deserialise(Arg.Any<string>()).Returns(x => DeserialisedMessage);
            SerialisationRegister.GetSerialiser(Arg.Any<string>()).Returns(Serialiser);
            MessageFootprintStore.IsMessageReceieved(Arg.Any<Guid>()).Returns(false);
        }

        protected override void When()
        {
            base.When();

            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateResponseMessage("anymessagetype", _messageId));
            
            Thread.Sleep(500);
        }

        [Then]
        public void MessageIsMarkedAsRecieved()
        {
            MessageFootprintStore.Received().MarkMessageAsRecieved(_messageId);
            
        }
    }
}