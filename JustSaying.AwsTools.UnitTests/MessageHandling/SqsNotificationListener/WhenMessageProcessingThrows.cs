
using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class BrokenMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public void BeforeGettingMoreMessages()
        {
            throw new Exception("Lol no");
        }

        public void ProcessMessage(Action action)
        {
            throw new Exception("Lol no");
        }
    }

    /// <summary>
    /// this test exercises different exception ahndlers to the "handler throws an exception" path in WhenMessageHandlingThrows
    /// </summary>
    public class WhenMessageProcessingThrows : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(true);
        }

        protected override void When()
        {
            SystemUnderTest.WithMessageProcessingStrategy(new BrokenMessageProcessingStrategy());
            base.When();
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
                () => Monitor.DidNotReceiveWithAnyArgs().HandleException(
                        Arg.Any<string>()));
        }
    }
}