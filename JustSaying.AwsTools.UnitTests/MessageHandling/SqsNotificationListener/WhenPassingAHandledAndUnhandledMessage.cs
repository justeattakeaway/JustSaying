using System.Threading;
using Amazon.SQS.Model;
using JustBehave;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenPassingAHandledAndUnhandledMessage : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null)
                .ReturnsForAnyArgs(info => true)
                .AndDoes(x => Thread.Sleep(1)); // Ensure at least one ms wait on processing
        }

        [Then]
        public void MessagesGetDeserialisedByCorrectHandler()
        {
            SerialisationRegister.Received().DeserializeMessage(
                SqsMessageBody(MessageTypeString));
        }

        [Then]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserialisedMessage);
        }

        [Then]
        public void MonitoringToldMessageHandlingTime()
        {
            Monitor.Received().HandleTime(Arg.Is<long>(x => x > 0));
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            SerialisationRegister.Received(2).DeserializeMessage(Arg.Any<string>());

            Sqs.Received().DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. etc
    */
}
