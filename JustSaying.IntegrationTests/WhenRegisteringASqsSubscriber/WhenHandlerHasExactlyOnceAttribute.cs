using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [ExactlyOnce(TimeOut = 10)]
    public class SampleHandler : IHandlerAsync<GenericMessage>
    {
        private int _count;
        public Task<bool> Handle(GenericMessage message)
        {
            Interlocked.Increment(ref _count);
            return Task.FromResult(true);
        }

        public int NumberOfTimesIHaveBeenCalled() => _count;
    }
    [ExactlyOnce]
    public class AnotherSampleHandler : SampleHandler { }

    [TestFixture]
    public class WhenTwoDifferentHanldersHandleAMessageWithExactlyOnceAttribute
    {
        private string _queueName;
        private GenericMessage _message;
        private SampleHandler _handler1;
        private SampleHandler _handler2;
        private const string Region = "eu-west-1";

        [SetUp]
        protected void SetUp()
        {
            _queueName = "queuename-" + DateTime.Now.Ticks;
            _message = new GenericMessage { Id = Guid.NewGuid() };
        }

        private async Task Act()
        {
            _handler1 = new SampleHandler();
            _handler2 = new AnotherSampleHandler();
            var publisher = await CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(Region)
                .ConfigurePublisherWith(_ => { })
                .WithSnsMessagePublisher<GenericMessage>()
                .Build();

            var bus = await CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(Region)
                .WithMonitoring(new Monitoring())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber().IntoQueue(_queueName)
                .WithMessageHandlers(_handler1, _handler2)
                .Build();

            publisher.StartListening();
            bus.StartListening();

            publisher.Publish(_message);
        }

        [Test, Ignore("waiting for 2 sid-by-side consumers bug to get fixed.")]
        public async Task BothHandlersAreTriggered()
        {
            await Act();

            Thread.Sleep(5.Seconds());
            Assert.That(_handler1.NumberOfTimesIHaveBeenCalled(), Is.EqualTo(1));
            Assert.That(_handler2.NumberOfTimesIHaveBeenCalled(), Is.EqualTo(1));
        }
    }
    [TestFixture]
    public class WhenHandlerHasExactlyOnceAttribute
    {
        private string _queueName;
        private GenericMessage _message;
        private SampleHandler _sampleHandler;
        private const string Region = "eu-west-1";

        [SetUp]
        protected void SetUp()
        {
            _queueName = "queuename-" + DateTime.Now.Ticks;
            _message = new GenericMessage{Id = Guid.NewGuid()};
        }

        private async Task Act()
        {
            _sampleHandler = new SampleHandler();
            var publisher = await CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(Region)
                .WithSnsMessagePublisher<GenericMessage>()
                .Build();

            var bus = await CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(Region)
                .WithMonitoring(new Monitoring())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber()
                .IntoQueue(_queueName)
                .ConfigureSubscriptionWith(cfg =>
                {
                    cfg.MessageRetentionSeconds = 60;
                }).WithMessageHandler(_sampleHandler)
                .Build();

            publisher.StartListening();
            bus.StartListening();

            publisher.Publish(_message);
            publisher.Publish(_message);
        }

        [Test]
        public async Task MessageHasBeenCalledOnce()
        {
            await Act();

            await Task.Delay(5.Seconds());
            Assert.That(_sampleHandler.NumberOfTimesIHaveBeenCalled(), Is.EqualTo(1));
        }
    }
    internal class MessageLockStore : IMessageLock
        {
            private readonly Dictionary<string, int> _store = new Dictionary<string, int>();
            public MessageLockResponse TryAquireLockPermanently(string key)
            {
                var canAquire = !_store.TryGetValue(key, out var _);
                if (canAquire) _store.Add(key, 1);
                return new MessageLockResponse {DoIHaveExclusiveLock = canAquire};
            }

            public MessageLockResponse TryAquireLock(string key, TimeSpan howLong) => TryAquireLockPermanently(key);

            public void ReleaseLock(string key) => _store.Remove(key);
        }
    internal class Monitoring : IMessageMonitor, IMeasureHandlerExecutionTime
        {
            public void HandleException(string messageType) { }
            public void HandleTime(long handleTimeMs) { }
            public void IssuePublishingMessage() { }
            public void IncrementThrottlingStatistic() { }
            public void HandleThrottlingTime(long handleTimeMs) { }
            public void PublishMessageTime(long handleTimeMs) { }
            public void ReceiveMessageTime(long handleTimeMs, string queueName, string region) { }
            public void HandlerExecutionTime(string typeName, string eventName, TimeSpan executionTime) { }
        }

}
