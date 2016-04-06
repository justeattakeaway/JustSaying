using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    /// <summary>
    /// this test exercises different exception handlers to the "handler throws an exception" path in WhenMessageHandlingThrows
    /// </summary>
    public class WhenMessageProcessingThrowsDuring : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(true);
        }

        protected override async Task When()
        {
            var doneSignal = new TaskCompletionSource<object>();
            SystemUnderTest.WithMessageProcessingStrategy(new ThrowingDuringMessageProcessingStrategy(doneSignal));

            SystemUnderTest.AddMessageHandler(() => Handler);
            SystemUnderTest.Listen();

            await doneSignal.Task;
            await SystemUnderTest.StopListening();
        }

        [Then]
        public void MessageHandlerWasNotCalled()
        {
            Handler.DidNotReceiveWithAnyArgs().Handle(Arg.Any<GenericMessage>());
        }

        [Then]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            Sqs.DidNotReceiveWithAnyArgs().DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }

        [Then]
        public void ExceptionIsLoggedToMonitor()
        {
            Monitor.DidNotReceiveWithAnyArgs().HandleException(Arg.Any<string>());
        }
    }
}