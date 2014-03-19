using System.Threading;
using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenPassingAHandledAndUnhandledMessage : BaseQueuePollingTest
    {
        protected override void Given()
        {
            TestWaitTime = 100;
            Handler.Handle(null).ReturnsForAnyArgs(info => true).AndDoes(x => Thread.Sleep(1)); // Ensure at least one ms wait on processing
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
        public void MonitoringToldMessageHandlingTime()
        {
            Thread.Sleep(50);
            Monitor.Received().HandleTime(Arg.Is<long>(x => x > 0));
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            Serialiser.Received(1).Deserialise(Arg.Any<string>());
            Sqs.Received(2).DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. etc
    */
}
