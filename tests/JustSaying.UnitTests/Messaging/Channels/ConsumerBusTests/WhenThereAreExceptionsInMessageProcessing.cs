using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests
{
    public class WhenThereAreExceptionsInMessageProcessing : BaseConsumerBusTests
    {
        private ISqsQueue _queue;
        private int _callCount;

        public WhenThereAreExceptionsInMessageProcessing(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue(() =>
            {
                Interlocked.Increment(ref _callCount);
                return new List<Message> { new TestMessage() };
            });

            Queues.Add(_queue);
            Handler.Handle(null).ReturnsForAnyArgs(true);

            SerializationRegister
                .DeserializeMessage(Arg.Any<string>())
                .Returns(x => throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing"));
        }

        protected override async Task WhenAsync()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            await SystemUnderTest.Start(cts.Token);
        }

        [Fact]
        public void TheListenerDoesNotDie()
        {
            _callCount.ShouldBeGreaterThanOrEqualTo(3);
        }
    }
}
