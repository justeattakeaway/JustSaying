using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenMessageHandlingFails : BaseQueuePollingTest
    {
        protected override void Given()
        {
            Handler.Handle(null).ReturnsForAnyArgs(false);
            TestWaitTime = 120;
            base.Given();
        }

        [Then]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            // The un-handled one is however.
            Sqs.Received(1).DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }
    }
}