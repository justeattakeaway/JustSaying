using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenThereAreExceptionsInSqsCalling : BaseQueuePollingTest
    {
        private int _sqsCallCounter;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        protected override void Given()
        {
            Sqs = Substitute.For<IAmazonSQS>();
            SerializationRegister = Substitute.For<IMessageSerializationRegister>();
            Monitor = Substitute.For<IMessageMonitor>();
            Handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            LoggerFactory = Substitute.For<ILoggerFactory>();
            GenerateResponseMessage(MessageTypeString, Guid.NewGuid());

            DeserializedMessage = new SimpleMessage { RaisingComponent = "Component" };

            Sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(_ =>  ExceptionOnFirstCall());
        }

        private Task ExceptionOnFirstCall()
        {
            _sqsCallCounter++;
            if (_sqsCallCounter == 1)
            {
                throw new TestException("testing the failure on first call");
            }
            if (_sqsCallCounter == 2)
            {
                TaskHelpers.DelaySendDone(_tcs);
            }

            return Task.FromResult(new ReceiveMessageResponse());
        }

        protected override async Task When()
        {
            SystemUnderTest.AddMessageHandler(() => Handler);
            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);

            // wait until it's done
            await TaskHelpers.WaitWithTimeoutAsync(_tcs.Task);
            cts.Cancel();
        }

        [Fact]
        public void QueueIsPolledMoreThanOnce()
        {
            _sqsCallCounter.ShouldBeGreaterThan(1);
        }
    }
}
