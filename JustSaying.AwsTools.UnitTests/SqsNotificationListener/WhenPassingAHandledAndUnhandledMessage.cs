using System.Threading;
using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenPassingAHandledAndUnhandledMessage : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(info => true).AndDoes(x => Thread.Sleep(1)); // Ensure at least one ms wait on processing
        }

        [Then]
        public void MessagesGetDeserialisedByCorrectHandler()
        {
            Patiently.VerifyExpectation(() =>Serialiser.Received().Deserialise(MessageBody));
        }

        [Then]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Patiently.VerifyExpectation(() =>Handler.Received().Handle(DeserialisedMessage));
        }

        [Then]
        public void MonitoringToldMessageHandlingTime()
        {
            Patiently.VerifyExpectation(() =>Monitor.Received().HandleTime(Arg.Is<long>(x => x > 0)));
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            Patiently.VerifyExpectation(() =>Serialiser.Received(1).Deserialise(Arg.Any<string>()));
            Patiently.VerifyExpectation(() =>Sqs.Received(2).DeleteMessage(Arg.Any<DeleteMessageRequest>()));
        }
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. etc
    */
}
