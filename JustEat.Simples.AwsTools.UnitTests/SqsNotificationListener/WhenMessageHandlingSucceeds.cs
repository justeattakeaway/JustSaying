using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenMessageHandlingSucceeds : BaseQueuePollingTest
    {
        protected override void Given()
        {
            TestWaitTime = 100;
            Handler.Handle(null).ReturnsForAnyArgs(true);
            base.Given();
        }

        [Then]
        public void MessagesGetDeserialisedByCorrectHandler()
        {
            Serialiser.Received().Deserialise(MessageBody);
        }

        [Then]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserialisedMessage);
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            Sqs.Received(2).DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }

        [Then]
        public void ReceiveMessageTimeStatsSent()
        {
            Monitor.Received().ReceiveMessageTime(Arg.Any<long>());
        }
    }
}