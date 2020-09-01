using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
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
        protected FakeSerializationRegister SerializationRegister;
        protected int ConcurrencyLimit = 8;

        protected FakeMessageLock MessageLock
        {
            get => (FakeMessageLock) HandlerMap.MessageLock;
            set => HandlerMap.MessageLock = value;
        }

        protected IHandlerAsync<SimpleMessage> Handler;

        protected ISubscriptionGroup SystemUnderTest { get; private set; }

        protected static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMilliseconds(500);
        private ITestOutputHelper _outputHelper;

        protected ILoggerFactory LoggerFactory { get; }
        protected ILogger Logger { get; }

        public BaseSubscriptionGroupTests(ITestOutputHelper testOutputHelper)
        {
            _outputHelper = testOutputHelper;
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
            SerializationRegister = new FakeSerializationRegister();
            HandlerMap = new HandlerMap(Monitor, LoggerFactory);

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

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var completion = SystemUnderTest.RunAsync(cts.Token);

            await Patiently.AssertThatAsync(_outputHelper, async () =>
            {
                await doneSignal.Task;
                return true;
            });

            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
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

        protected static ISqsQueue CreateSuccessfulTestQueue(string queueName, params Message[] messages)
        {
            return CreateSuccessfulTestQueue(queueName, () => messages.ToList());
        }

        protected static ISqsQueue CreateSuccessfulTestQueue(string queueName, Func<List<Message>> getMessages)
        {
            return CreateSuccessfulTestQueue(queueName, () => Task.FromResult(getMessages()));
        }

        protected static ISqsQueue CreateSuccessfulTestQueue(string queueName, Func<Task<List<Message>>> getMessages)
        {
            var client = Substitute.For<IAmazonSQS>();
            client
                .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(async _ =>
                {
                    Thread.Sleep(30);
                    var messages = await getMessages();

                    return new ReceiveMessageResponse
                    {
                        Messages = messages,
                    };
                });

            var queue = Substitute.For<ISqsQueue>();

            queue.Client.Returns(client);
            queue.Uri.Returns(new Uri("http://foo.com"));
            queue.QueueName.Returns(queueName);

            return queue;
        }

        public Task DisposeAsync()
        {
            LoggerFactory?.Dispose();

            return Task.CompletedTask;
        }

        protected class TestMessage : Message
        {
        }
    }
}
