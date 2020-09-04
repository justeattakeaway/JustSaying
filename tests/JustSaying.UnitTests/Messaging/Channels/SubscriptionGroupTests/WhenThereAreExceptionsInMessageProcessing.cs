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
        { }

        protected override void Given()
        {
            ConcurrencyLimit = 1;
            _queue = CreateSuccessfulTestQueue("TestQueue",
                EnumerableExtensions.GenerateInfinite(() =>
                {
                    Interlocked.Increment(ref _callCount);
                    return new ReceiveMessageResponse()
                    {
                        Messages = new List<Message> { new TestMessage() }
                    };
                }));

            Queues.Add(_queue);

            SerializationRegister.DefaultDeserializedMessage = () =>
                throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing");
        }

        [Fact]
        public async Task TheListenerDoesNotDie()
        {
            await Patiently.AssertThatAsync(OutputHelper,
                () => _callCount.ShouldBeGreaterThan(1));
        }
    }
}
