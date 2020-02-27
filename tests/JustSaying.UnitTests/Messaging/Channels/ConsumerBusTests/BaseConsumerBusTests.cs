using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener.Support;
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

        protected IConsumerBus SystemUnderTest { get; private set; }

        protected readonly ILoggerFactory LoggerFactory;

        public BaseConsumerBusTests(ITestOutputHelper testOutputHelper)
        {
            LoggerFactory = testOutputHelper.ToLoggerFactory();
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
            SystemUnderTest.Start(cts.Token);

            // wait until it's done
            var doneOk = await TaskHelpers.WaitWithTimeoutAsync(doneSignal.Task);

            cts.Cancel();

            await SystemUnderTest.Completion;

            doneOk.ShouldBeTrue("Timeout occured before done signal");
        }

        protected IConsumerBus CreateSystemUnderTest()
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

            var bus = new ConsumerBus(
                Queues,
                1,
                dispatcher,
                Monitor,
                LoggerFactory);

            return bus;
        }

        public Task DisposeAsync()
        {
            LoggerFactory?.Dispose();

            return Task.CompletedTask;
        }

        protected class TestMessage : Amazon.SQS.Model.Message { }
    }
}
