using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener.Support;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
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
            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);

            await Tasks.WaitWithTimeoutAsync(doneSignal.Task);

            cts.Cancel();
            await Task.Yield();
        }

        [Fact]
        public void MessageHandlerWasNotCalled()
        {
            Handler.DidNotReceiveWithAnyArgs().Handle(Arg.Any<SimpleMessage>());
        }

        [Fact]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            Sqs.DidNotReceiveWithAnyArgs().DeleteMessageAsync(Arg.Any<DeleteMessageRequest>());
        }

        [Fact]
        public void ExceptionIsLoggedToMonitor()
        {
            Monitor.DidNotReceiveWithAnyArgs().HandleException(Arg.Any<Type>());
        }
    }
}
