using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [ExactlyOnce(TimeOut = 10)]
    public class SampleHandler : IHandlerAsync<SimpleMessage>
    {
        private int _count;
        public Task<bool> Handle(SimpleMessage message)
        {
            Interlocked.Increment(ref _count);
            return Task.FromResult(true);
        }

        public int NumberOfTimesIHaveBeenCalled()
        {
            return _count;
        }
    }

    [ExactlyOnce]
    public class AnotherSampleHandler : SampleHandler { }

    [Collection(GlobalSetup.CollectionName)]
    public class WhenTwoDifferentHanldersHandleAMessageWithExactlyOnceAttribute
    {
        protected string QueueName;
        private readonly SimpleMessage _message;
        private SampleHandler _handler1;
        private SampleHandler _handler2;
        private const string region = "eu-west-1";
        
        public WhenTwoDifferentHanldersHandleAMessageWithExactlyOnceAttribute()
        {
            QueueName = "queuename-" + DateTime.Now.Ticks;
            _message = new SimpleMessage { Id = Guid.NewGuid() };
        }

        protected async Task Act()
        {
            _handler1 = new SampleHandler();
            _handler2 = new AnotherSampleHandler();
            var publisher = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(region)
                .ConfigurePublisherWith(_ => { })
                .WithSnsMessagePublisher<SimpleMessage>();

            var bus = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(region)
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber().IntoQueue(QueueName)
                .WithMessageHandlers(_handler1, _handler2);

            publisher.StartListening();
            bus.StartListening();

            await publisher.PublishAsync(_message);
        }

        [Fact]
        public async Task BothHandlersAreTriggered()
        {
            await Act();

            // TODO Should only need to do this in real AWS
            await Task.Delay(5.Seconds());

            _handler1.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
            _handler2.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
        }
    }

    [Collection(GlobalSetup.CollectionName)]
    public class WhenHandlerHasExactlyOnceAttribute
    {
        protected string QueueName;
        private readonly SimpleMessage _message;
        private SampleHandler _sampleHandler;
        private const string region = "eu-west-1";
        
        public WhenHandlerHasExactlyOnceAttribute()
        {
            QueueName = "queuename-" + DateTime.Now.Ticks;
            _message = new SimpleMessage { Id = Guid.NewGuid()};
        }

        protected async Task Act()
        {
            _sampleHandler = new SampleHandler();
            var publisher = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(region)
                .WithSnsMessagePublisher<SimpleMessage>();

            var bus = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(region)
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .ConfigureSubscriptionWith(cfg =>
                {
                    cfg.MessageRetentionSeconds = 60;
                }).WithMessageHandler(_sampleHandler);
            publisher.StartListening();
            bus.StartListening();

            await publisher.PublishAsync(_message);
            await publisher.PublishAsync(_message);
        }

        [Fact]
        public async Task MessageHasBeenCalledOnce()
        {
            await Act();

            // TODO Only need to do this in real AWS
            await Task.Delay(5.Seconds());

            _sampleHandler.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
        }
    }

    internal class MessageLockStore : IMessageLock
    {
        private readonly Dictionary<string, int> _store = new Dictionary<string, int>();

        public MessageLockResponse TryAquireLockPermanently(string key)
        {
            int value;
            var canAquire = !_store.TryGetValue(key, out value);
            if (canAquire)
                _store.Add(key, 1);
            return new MessageLockResponse {DoIHaveExclusiveLock = canAquire};
        }

        public MessageLockResponse TryAquireLock(string key, TimeSpan howLong)
        {
            return TryAquireLockPermanently(key);
        }

        public void ReleaseLock(string key)
        {
            _store.Remove(key);
        }
    }
}
