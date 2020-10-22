using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware.ExactlyOnce;
using JustSaying.Messaging.Middleware.Handle;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenExactlyOnceIsAppliedWithoutSpecificTimeout : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private readonly int _maximumTimeout = (int)TimeSpan.MaxValue.TotalSeconds;
        private FakeMessageLock _messageLock;

        public WhenExactlyOnceIsAppliedWithoutSpecificTimeout(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());
            Queues.Add(_queue);
            _messageLock = new FakeMessageLock();

            var servicesBuilder = new ServicesBuilder(new MessagingBusBuilder());
            var serviceResolver = new FakeServiceResolver(sc =>
                sc.AddSingleton<IMessageLockAsync>(_messageLock)
                    .AddSingleton<IHandlerAsync<SimpleMessage>>(Handler)
                    .AddLogging(x => x.AddXUnit(OutputHelper)));

            var middlewareBuilder = new HandlerMiddlewareBuilder(serviceResolver, serviceResolver, servicesBuilder);

            var middleware = middlewareBuilder.Configure(pipe =>
            {
                pipe.UseExactlyOnce<SimpleMessage>("a-unique-lock-key");
                pipe.UseHandler<SimpleMessage>();
            }).Build();

            Middleware = middleware;
        }

        protected override async Task WhenAsync()
        {
            MiddlewareMap.Add<SimpleMessage>(_queue.QueueName, () => Middleware);

            var cts = new CancellationTokenSource();

            var completion = SystemUnderTest.RunAsync(cts.Token);

            await Patiently.AssertThatAsync(OutputHelper,
                () => Handler.ReceivedMessages.Any());

            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);

        }

        [Fact]
        public void MessageIsLocked()
        {
            var messageId = SerializationRegister.DefaultDeserializedMessage().Id.ToString();

            var tempLockRequests = _messageLock.MessageLockRequests.Where(lr => !lr.isPermanent);
            tempLockRequests.ShouldNotBeEmpty();

            foreach(var lockRequest in tempLockRequests)
            {
                lockRequest.key.ShouldContain(messageId, Case.Insensitive);
                ((int)lockRequest.howLong.TotalSeconds).ShouldBe(_maximumTimeout);
            }
        }
    }
}
