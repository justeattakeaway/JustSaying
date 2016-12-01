using Amazon.SQS.Model;
using JustBehave;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
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
            SerialisationRegister.Received().DeserializeMessage(SqsMessageBody(MessageTypeString));
        }

        [Then]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserialisedMessage);
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            Sqs.Received(2).DeleteMessageAsync(Arg.Any<DeleteMessageRequest>());
        }

        [Then]
        public void ReceiveMessageTimeStatsSent()
        {
            Monitor.Received().ReceiveMessageTime(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Then]
        public void ExceptionIsNotLoggedToMonitor()
        {
            Monitor.DidNotReceiveWithAnyArgs().HandleException(Arg.Any<string>());
        }
    }
}
