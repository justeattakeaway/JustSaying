using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    [ExactlyOnce(TimeOut = 5)]
    public class ExactlyOnceHandler : InspectableHandler<SimpleMessage>
    {

    }

    public class WhenExactlyOnceIsAppliedToHandler : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private readonly int _expectedTimeout = 5;

        public WhenExactlyOnceIsAppliedToHandler(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        { }

        protected override void Given()
        {
            Handler = new ExactlyOnceHandler();

            _queue = CreateSuccessfulTestQueue("TestQueue", new TestMessage());

            Queues.Add(_queue);

            MessageLock = new FakeMessageLock();
        }

        protected override async Task WhenAsync()
        {
            HandlerMap.Add(_queue.QueueName, () => Handler);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            var completion = SystemUnderTest.RunAsync(cts.Token);

            // wait until it's done
            await Patiently.AssertThatAsync(OutputHelper,
                () => Handler.ReceivedMessages.Any());

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
        }

        [Fact]
        public void ProcessingIsPassedToTheHandler()
        {
            Handler.ReceivedMessages.ShouldNotBeEmpty();
        }

        [Fact]
        public void MessageIsLocked()
        {
            var messageId = SerializationRegister.DefaultDeserializedMessage().Id.ToString();

            var tempLockRequests = MessageLock.MessageLockRequests.Where(lr => !lr.isPermanent);
            tempLockRequests.Count().ShouldBeGreaterThan(0);
            tempLockRequests.ShouldAllBe(pair =>
                pair.key.Contains(messageId, StringComparison.OrdinalIgnoreCase) &&
                pair.howLong == TimeSpan.FromSeconds(_expectedTimeout));
        }
    }
}
