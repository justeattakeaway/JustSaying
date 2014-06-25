using System;
using System.Threading;
using Amazon.SQS.Model;
using AwsTools.UnitTests.MessageStubs;
using JustEat.Testing;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenAMessageIsReceievedMoreThanOnce : BaseQueuePollingTest
    {
        private string _messageType;
        private Guid _messageId;

        protected override void Given()
        {
            base.Given();
            _messageId = Guid.NewGuid();
            DeserialisedMessage = new GenericMessage { Id = _messageId };
            Serialiser.Deserialise(Arg.Any<string>()).Returns(x => DeserialisedMessage);
            SerialisationRegister.GetSerialiser(Arg.Any<string>()).Returns(Serialiser);
        }
        protected override void When()
        {
            base.When();
            
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateResponseMessage(_messageType, _messageId));
            _messageType = "anymessagetype";
        }
        
        [Then]
        public void MessageIsNotReceivedAgain()
        {
            Patiently.VerifyExpectation(() => Handler.DidNotReceive().Handle(Arg.Any<GenericMessage>()));
        }
    }
}