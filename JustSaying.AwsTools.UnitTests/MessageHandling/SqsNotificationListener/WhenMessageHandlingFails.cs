using System.Threading.Tasks;
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
        public async Task MessageHandlerWasCalled()
        {
            await Patiently.VerifyExpectationAsync(
                () => Handler.ReceivedWithAnyArgs().Handle(
                        Arg.Any<GenericMessage>()));
        }

        [Then]
        public async Task FailedMessageIsNotRemovedFromQueue()
        {
            // The un-handled one is however.
            await Patiently.VerifyExpectationAsync(
                () => Sqs.DidNotReceiveWithAnyArgs().DeleteMessage(
                        Arg.Any<DeleteMessageRequest>()));
        }

        [Then]
        public async Task ExceptionIsNotLoggedToMonitor()
        {
            await Patiently.VerifyExpectationAsync(
                () => Monitor.DidNotReceiveWithAnyArgs().HandleException(
                        Arg.Any<string>()));
        }

    }
}