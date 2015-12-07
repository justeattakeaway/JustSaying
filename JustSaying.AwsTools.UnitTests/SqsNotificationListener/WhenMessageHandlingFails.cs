using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using NSubstitute;
using JustSaying.TestingFramework;

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
        public async Task FailedMessageIsNotRemovedFromQueue()
        {
            // The un-handled one is however.
            await Patiently.VerifyExpectationAsync(
                () => Sqs.Received(1).DeleteMessage(
                        Arg.Any<DeleteMessageRequest>()));
        }
    }
}