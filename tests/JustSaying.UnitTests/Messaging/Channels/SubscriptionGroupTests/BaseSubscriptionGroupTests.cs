using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public abstract class BaseSubscriptionGroupTests : IAsyncLifetime
    {
        protected IList<ISqsQueue> Queues;
        protected HandlerMap HandlerMap;
        protected IMessageMonitor Monitor;
        protected SimpleMessage DeserializedMessage;
        protected IMessageSerializationRegister SerializationRegister;
        protected int ConcurrencyLimit = 8;

        protected IMessageLockAsync MessageLock
        {
            get => HandlerMap.MessageLock;
            set => HandlerMap.MessageLock = value;
        }

        protected IHandlerAsync<SimpleMessage> Handler;

        protected ISubscriptionGroup SystemUnderTest { get; private set; }

        protected static readonly TimeSpan TimeoutPeriod = TimeSpan.FromSeconds(1);

        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Logger { get; }

        public BaseSubscriptionGroupTests(ITestOutputHelper testOutputHelper)
        {
            LoggerFactory = testOutputHelper.ToLoggerFactory();
            Logger = LoggerFactory.CreateLogger(GetType());
        }

        public async Task InitializeAsync()
        {
            GivenInternal();

            SystemUnderTest = CreateSystemUnderTest();

            await WhenAsync().ConfigureAwait(false);
        }

        private void GivenInternal()
        {
            Queues = new List<ISqsQueue>();
            Handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            Monitor = Substitute.For<IMessageMonitor>();
            SerializationRegister = Substitute.For<IMessageSerializationRegister>();
            HandlerMap = new HandlerMap(Monitor, LoggerFactory);

            DeserializedMessage = new SimpleMessage { RaisingComponent = "Component" };
            SerializationRegister.DeserializeMessage(Arg.Any<string>()).Returns((DeserializedMessage, new MessageAttributes()));

            Given();
        }

        protected abstract void Given();

        // Default implementation
        protected virtual async Task WhenAsync()
        {
            var doneSignal = new TaskCompletionSource<object>();
            var signallingHandler = new SignallingHandler<SimpleMessage>(doneSignal, Handler);

            foreach (ISqsQueue queue in Queues)
            {
                HandlerMap.Add(queue.QueueName, typeof(SimpleMessage), msg => signallingHandler.Handle(msg as SimpleMessage));
            }

            var cts = new CancellationTokenSource();
            var completion = SystemUnderTest.RunAsync(cts.Token);

            // wait until it's done
            var doneOk = await TaskHelpers.WaitWithTimeoutAsync(doneSignal.Task);

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);

            doneOk.ShouldBeTrue("Timeout occured before done signal");
        }

        protected ISubscriptionGroup CreateSystemUnderTest()
        {
            var messageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();
            var messageContextAccessor = Substitute.For<IMessageContextAccessor>();

            var dispatcher = new MessageDispatcher(
                SerializationRegister,
                Monitor,
                HandlerMap,
                LoggerFactory,
                messageBackoffStrategy,
                messageContextAccessor);

            var defaults = new SubscriptionGroupSettingsBuilder()
                .WithDefaultConcurrencyLimit(ConcurrencyLimit);

            var subscriptionGroupFactory = new SubscriptionGroupFactory(
                dispatcher,
                Monitor,
                LoggerFactory);

            var settings = SetupBusConfig();

            return subscriptionGroupFactory.Create(defaults, settings);
        }

        protected virtual Dictionary<string, SubscriptionGroupConfigBuilder> SetupBusConfig()
        {
            return new Dictionary<string, SubscriptionGroupConfigBuilder>
            {
                { "test", new SubscriptionGroupConfigBuilder("test").AddQueues(Queues) },
            };
        }

        protected static ISqsQueue CreateSuccessfulTestQueue(string queueName, params Amazon.SQS.Model.Message[] messages)
        {
            return CreateSuccessfulTestQueue(queueName, () => messages);
        }

        protected static ISqsQueue CreateSuccessfulTestQueue(string queueName, Func<IList<Amazon.SQS.Model.Message>> getMessages)
        {
            return CreateSuccessfulTestQueue(queueName, () => Task.FromResult(getMessages()));
        }

        protected static ISqsQueue CreateSuccessfulTestQueue(string queueName, Func<Task<IList<Amazon.SQS.Model.Message>>> getMessages)
        {
            var queue = Substitute.For<ISqsQueue>();
            queue.GetMessagesAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ => getMessages());
            queue.Uri.Returns(new Uri("http://foo.com"));
            queue.QueueName.Returns(queueName);

            return queue;
        }

        public Task DisposeAsync()
        {
            LoggerFactory?.Dispose();

            return Task.CompletedTask;
        }

        protected class TestMessage : Amazon.SQS.Model.Message
        {
        }
    }
}
