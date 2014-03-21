using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;
using SimpleMessageMule.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenMessageHandlingFails : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(false);
        }

        [Then]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            // The un-handled one is however.
            Patiently.VerifyExpectation(() => Sqs.Received(1).DeleteMessage(Arg.Any<DeleteMessageRequest>()));
        }
    }
}