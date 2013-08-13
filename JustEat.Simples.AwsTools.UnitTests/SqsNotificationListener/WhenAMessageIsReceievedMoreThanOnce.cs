using System;
using System.Threading;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;
using JustEat.Simples.NotificationStack.Messaging.Messages.Sms;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenAMessageIsReceievedMoreThanOnce : BaseQueuePollingTest
    {
        private string _messageType;
        private Guid _messageId;

        protected override void Given()
        {
            _messageId = Guid.NewGuid();
            DeserialisedMessage = new CustomerOrderRejectionSms(1, 2, "3", SmsCommunicationActivity.ConfirmedReceived){Id = _messageId};
            Serialiser.Deserialise(Arg.Any<string>()).Returns(x => DeserialisedMessage);
            SerialisationRegister.GetSerialiser(Arg.Any<string>()).Returns(Serialiser);
            
            MessageFootprintStore.IsMessageReceieved(Arg.Any<Guid>()).Returns(true);
        }
        protected override void When()
        {
            base.When();
            
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateResponseMessage(_messageType, _messageId));
            _messageType = "anymessagetype";
            Thread.Sleep(500);
        }
        
        [Then]
        public void MessageIsNotReceivedAgain()
        {
            Handler.DidNotReceive().Handle(Arg.Any<CustomerOrderRejectionSms>());
        }
    }
}