using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenMessageHandlingThrows : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(Arg.Any<GenericMessage>()).ThrowsForAnyArgs(new Exception("handler went rong"));
        }

        [Then]
        public async Task FailedMessageIsNotRemovedFromQueue()
        {
            await Patiently.VerifyExpectationAsync(
                () => Sqs.DidNotReceiveWithAnyArgs().DeleteMessage(
                        Arg.Any<DeleteMessageRequest>()));
        }

        [Then]
        public async Task ExceptionIsLoggedToMonitor()
        {
            await Patiently.VerifyExpectationAsync(
                () => Monitor.ReceivedWithAnyArgs().HandleException(
                        Arg.Any<string>()));
        }
    }
}