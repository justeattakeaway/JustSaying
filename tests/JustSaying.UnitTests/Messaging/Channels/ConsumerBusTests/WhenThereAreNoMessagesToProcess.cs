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
    public class WhenThereAreNoMessagesToProcess : BaseConsumerBusTests
    {
        private ISqsQueue _queue;
        private int _callCount = 0;

        public WhenThereAreNoMessagesToProcess(ITestOutputHelper testOutputHelper)
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
            Handler.Handle(Arg.Any<SimpleMessage>()).ReturnsForAnyArgs(false);
        }

        protected override async Task WhenAsync()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            SystemUnderTest.Start(cts.Token);

            await SystemUnderTest.Completion;
        }

        [Fact]
        public void ListenLoopDoesNotDie()
        {
            _callCount.ShouldBeGreaterThan(3);
        }
    }
}
