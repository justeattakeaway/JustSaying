using System;
using Amazon.SQS.Model;
using AwsTools.UnitTests.MessageStubs;
using JustEat.Testing;
using NSubstitute;
using SimpleMessageMule.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenAMessageIsReceieved : BaseQueuePollingTest
    {
        private Guid _messageId;

        protected override void Given()
        {
            base.Given();
            _messageId = Guid.NewGuid();
            DeserialisedMessage = new GenericMessage { Id = _messageId };
            Serialiser.Deserialise(Arg.Any<string>()).Returns(x => DeserialisedMessage);
            SerialisationRegister.GetSerialiser(Arg.Any<string>()).Returns(Serialiser);
            MessageFootprintStore.IsMessageReceieved(Arg.Any<Guid>()).Returns(false);
        }

        protected override void When()
        {
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateResponseMessage("anymessagetype", _messageId));
            base.When();
        }

        [Then]
        public void MessageIsMarkedAsRecieved()
        {
            Patiently.VerifyExpectation(() => MessageFootprintStore.Received().MarkMessageAsRecieved(_messageId));
        }

        
        

        
    }
}