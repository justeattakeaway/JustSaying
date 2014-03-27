using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;
using SimpleMessageMule.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenMessageHandlingSucceeds : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(true);
        }

        [Then]
        public void MessagesGetDeserialisedByCorrectHandler()
        {
            Patiently.VerifyExpectation(() => Serialiser.Received().Deserialise(MessageBody));
        }

        [Then]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Patiently.VerifyExpectation(() => Handler.Received().Handle(DeserialisedMessage));
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            Patiently.VerifyExpectation(() => Sqs.Received(2).DeleteMessage(Arg.Any<DeleteMessageRequest>()));
        }

        [Then]
        public void ReceiveMessageTimeStatsSent()
        {
            Patiently.VerifyExpectation(() => Monitor.Received().ReceiveMessageTime(Arg.Any<long>()));
        }
    }
}