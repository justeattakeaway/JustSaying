using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    [Ignore("Breaks in appveyor due to known nunit integration issues http://help.appveyor.com/discussions/problems/1737-tests-fail-to-run-with-weird-error")]
    public class WhenMessageHandlingThrows : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(Arg.Any<GenericMessage>()).ThrowsForAnyArgs(new Exception("Thrown by test handler"));
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