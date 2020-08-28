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

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenThereAreExceptionsInMessageProcessing : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private int _callCount;

        public WhenThereAreExceptionsInMessageProcessing(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            ConcurrencyLimit = 1;
            _queue = CreateSuccessfulTestQueue("TestQueue", () =>
            {
                Interlocked.Increment(ref _callCount);
                return new List<Message> { new TestMessage() };
            });

            Queues.Add(_queue);
            Handler.Handle(null).ReturnsForAnyArgs(true);

            SerializationRegister.DefaultDeserializedMessage = () =>
                throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing");
        }

        protected override async Task WhenAsync()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var completion = SystemUnderTest.RunAsync(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
        }

        [Fact]
        public void TheListenerDoesNotDie()
        {
            _callCount.ShouldBeGreaterThan(1);
        }
    }
}
