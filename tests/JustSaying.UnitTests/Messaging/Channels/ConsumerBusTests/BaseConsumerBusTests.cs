using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Configuration;
using JustSaying.Messaging.Channels.ConsumerGroups;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests
{
    public abstract class BaseConsumerBusTests : IAsyncLifetime
    {
        protected IList<ISqsQueue> Queues;
        protected int NumberOfConsumers;
        protected HandlerMap HandlerMap;
        protected IMessageMonitor Monitor;
        protected SimpleMessage DeserializedMessage;
        protected IMessageSerializationRegister SerializationRegister;

        protected IMessageLockAsync MessageLock
        {
            get => HandlerMap.MessageLock;
            set => HandlerMap.MessageLock = value;
        }

        protected IHandlerAsync<SimpleMessage> Handler;

        protected IConsumerGroup SystemUnderTest { get; private set; }

        protected static readonly TimeSpan TimeoutPeriod = TimeSpan.FromSeconds(1);

        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Logger { get; }

        public BaseConsumerBusTests(ITestOutputHelper testOutputHelper)
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
            NumberOfConsumers = 1;
            Handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            Monitor = Substitute.For<IMessageMonitor>();
            SerializationRegister = Substitute.For<IMessageSerializationRegister>();
            HandlerMap = new HandlerMap(Monitor, LoggerFactory);

            DeserializedMessage = new SimpleMessage { RaisingComponent = "Component" };
            SerializationRegister.DeserializeMessage(Arg.Any<string>()).Returns(DeserializedMessage);

            Given();
        }

        protected abstract void Given();

        // Default implementation
        protected virtual async Task WhenAsync()
        {
            var doneSignal = new TaskCompletionSource<object>();
            var signallingHandler = new SignallingHandler<SimpleMessage>(doneSignal, Handler);

            HandlerMap.Add(typeof(SimpleMessage), msg => signallingHandler.Handle(msg as SimpleMessage));

            var cts = new CancellationTokenSource();
            var completion = SystemUnderTest.Run(cts.Token);

            // wait until it's done
            var doneOk = await TaskHelpers.WaitWithTimeoutAsync(doneSignal.Task);

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);

            doneOk.ShouldBeTrue("Timeout occured before done signal");
        }

        protected IConsumerGroup CreateSystemUnderTest()
        {
            var messageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();
            var messageContextAccessor = Substitute.For<IMessageContextAccessor>();

            var dispatcher = new MessageDispatcher(
                SerializationRegister,
                Monitor,
                null,
                HandlerMap,
                LoggerFactory,
                messageBackoffStrategy,
                messageContextAccessor);

            var config = new ConsumerGroupConfig();
            config.WithDefaultSqsPolicy(LoggerFactory);

            var receiveBufferFactory = new ReceiveBufferFactory(LoggerFactory, config, Monitor);
            var multiplexerFactory = new MultiplexerFactory(LoggerFactory);
            var consumerFactory = new ChannelConsumerFactory(dispatcher);
            var consumerBusFactory = new SingleConsumerGroupFactory(
                multiplexerFactory, receiveBufferFactory, consumerFactory, LoggerFactory);

            var consumerGroupSettings = config.CreateConsumerGroupSettings(Queues);
            var settings = new Dictionary<string, ConsumerGroupSettings>
            {
                { "test", consumerGroupSettings },
            };

            var bus = new CombinedConsumerGroup(
                consumerBusFactory,
                settings,
                LoggerFactory.CreateLogger<CombinedConsumerGroup>());

            return bus;
        }

        protected static ISqsQueue CreateSuccessfulTestQueue(params Amazon.SQS.Model.Message[] messages)
        {
            return CreateSuccessfulTestQueue(() => messages);
        }

        protected static ISqsQueue CreateSuccessfulTestQueue(Func<IList<Amazon.SQS.Model.Message>> getMessages)
        {
            return CreateSuccessfulTestQueue(() => Task.FromResult(getMessages()));
        }

        protected static ISqsQueue CreateSuccessfulTestQueue(Func<Task<IList<Amazon.SQS.Model.Message>>> getMessages)
        {
            var queue = Substitute.For<ISqsQueue>();
            queue.GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ => getMessages());
            queue.Uri.Returns(new Uri("http://foo.com"));

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
