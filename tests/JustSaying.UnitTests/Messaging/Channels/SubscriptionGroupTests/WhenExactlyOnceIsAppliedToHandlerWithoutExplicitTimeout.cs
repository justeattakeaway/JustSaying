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
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class TestServiceResolver : IServiceResolver, IHandlerResolver
    {
        private readonly IServiceProvider _provider;

        public TestServiceResolver(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
        {
            return (IHandlerAsync<T>) _provider.GetService(typeof(IHandlerAsync<T>));
        }

        public T ResolveService<T>()
        {
            return _provider.GetService<T>();
        }
    }

    public class WhenExactlyOnceIsAppliedWithoutSpecificTimeout : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private readonly int _maximumTimeout = (int)TimeSpan.MaxValue.TotalSeconds;
        private ExactlyOnceSignallingHandler _handler;

        public WhenExactlyOnceIsAppliedWithoutSpecificTimeout(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());

            Queues.Add(_queue);

            MessageLock = new FakeMessageLock();

            var sc = new ServiceCollection()
                .AddSingleton(new ExactlyOnceMiddleware<SimpleMessage>(MessageLock,
                        TimeSpan.MaxValue,
                        nameof(ExactlyOnceSignallingHandler),
                        Logger))
                .AddSingleton<HandleMiddlewareBuilder>();

            var serviceResolver = new TestServiceResolver(sc.BuildServiceProvider());

            var middleware = new HandleMiddlewareBuilder(serviceResolver, serviceResolver)
                .Configure(pipe =>
                {
                    pipe.Use((service, next) => service.ResolveService<ExactlyOnceMiddleware<SimpleMessage>>());
                    pipe.UseHandlerMiddleware<SimpleMessage>(serviceResolver);
                })
                .Build<SimpleMessage>();

            Middleware = middleware;
        }

        protected override async Task WhenAsync()
        {
            MiddlewareMap.Add<SimpleMessage>(_queue.QueueName, () => Middleware);

            var cts = new CancellationTokenSource();

            var completion = SystemUnderTest.RunAsync(cts.Token);

            await Patiently.AssertThatAsync(OutputHelper,
                () => Middleware.Handler.ReceivedMessages.Any());

            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);

        }

        [Fact]
        public void MessageIsLocked()
        {
            var messageId = SerializationRegister.DefaultDeserializedMessage().Id.ToString();

            var tempLockRequests = MessageLock.MessageLockRequests.Where(lr => !lr.isPermanent);
            tempLockRequests.Count().ShouldBeGreaterThan(0);
            tempLockRequests.ShouldAllBe(pair =>
                pair.key.Contains(messageId, StringComparison.OrdinalIgnoreCase) &&
                pair.howLong == TimeSpan.FromSeconds(_maximumTimeout));
        }
    }
}
