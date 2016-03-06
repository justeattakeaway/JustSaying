using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenMessageHandlingFails : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(Arg.Any<GenericMessage>()).ReturnsForAnyArgs(false);
        }

        [Then]
        public void MessageHandlerWasCalled()
        {
            Handler.ReceivedWithAnyArgs().Handle(Arg.Any<GenericMessage>());
        }

        [Then]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            // The un-handled one is however.
            Sqs.DidNotReceiveWithAnyArgs().DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }

        [Then]
        public void ExceptionIsNotLoggedToMonitor()
        {
            Monitor.DidNotReceiveWithAnyArgs().HandleException(Arg.Any<string>());
        }
    }
}