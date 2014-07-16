using System;
using System.Collections.Generic;
using System.Threading;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [ExactlyOnce]
    public class SampleHandler : IHandler<GenericMessage>
    {
        private int _count;
        public bool Handle(GenericMessage message)
        {
            Interlocked.Increment(ref _count);
            return true;
        }

        public int NumberOfTimesIHaveBeenCalled()
        {
            return _count;
        }
    }
    [ExactlyOnce]
    public class AnotherSampleHandler : SampleHandler { }

    [TestFixture]
    public class WhenTwoDifferentHanldersHandleAMessageWithExactlyOnceAttribute
    {
        protected string TopicName;
        protected string QueueName;
        private GenericMessage _message;
        private SampleHandler _handler1;
        private SampleHandler _handler2;
        private const string region = "eu-west-1";

        [SetUp]
        protected void SetUp()
        {
            TopicName = "genericmessage";
            QueueName = "queuename-" + DateTime.Now.Ticks;
            _message = new GenericMessage { Id = Guid.NewGuid() };
        }
        protected void Act()
        {
            _handler1 = new SampleHandler();
            _handler2 = new AnotherSampleHandler();
            var publisher = CreateMeABus.InRegion(region)
                .ConfigurePublisherWith(_ => { })
                .WithSnsMessagePublisher<GenericMessage>(TopicName);

            var bus = CreateMeABus.InRegion(region)
                .WithMonitoring(new Monitoring())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber(TopicName).IntoQueue(QueueName)
                .WithMessageHandler(_handler1)
                .WithMessageHandler(_handler2);
            publisher.StartListening();
            bus.StartListening();

            publisher.Publish(_message);
        }

        [Test]
        public void BothHandlersAreTriggered()
        {
            Act();

            Thread.Sleep(5.Seconds());
            Assert.That(_handler1.NumberOfTimesIHaveBeenCalled(), Is.EqualTo(1));
            Assert.That(_handler2.NumberOfTimesIHaveBeenCalled(), Is.EqualTo(1));
        }
    }
    [TestFixture]
    public class WhenHandlerHasExactlyOnceAttribute
    {
        protected string TopicName;
        protected string QueueName;
        private GenericMessage _message;
        private SampleHandler _sampleHandler;
        private const string region = "eu-west-1";

        [SetUp]
        protected void SetUp()
        {
            TopicName = "CustomerCommunication";
            QueueName = "queuename-" + DateTime.Now.Ticks;
            _message = new GenericMessage{Id = Guid.NewGuid()};
        }
        protected void Act()
        {
            _sampleHandler = new SampleHandler();
            var publisher = CreateMeABus.InRegion(region)
                .WithSnsMessagePublisher<GenericMessage>();

            var bus = CreateMeABus.InRegion(region)
                .WithMonitoring(new Monitoring())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .ConfigureSubscriptionWith(cfg =>
                {
                    cfg.MessageRetentionSeconds = 60;
                }).WithMessageHandler(_sampleHandler);
            publisher.StartListening();
            bus.StartListening();

            publisher.Publish(_message);
            publisher.Publish(_message);
        }

        [Test]
        public void MessageHasBeenCalledOnce()
        {
            Act();
            
            Thread.Sleep(5.Seconds());
            Assert.That(_sampleHandler.NumberOfTimesIHaveBeenCalled(), Is.EqualTo(1));
        }

        
    }
    internal class MessageLockStore : IMessageLock
        {
            private readonly Dictionary<string, int> _store = new Dictionary<string, int>();
            public bool TryAquire(string key)
            {
                int value;
                bool canAquire = !_store.TryGetValue(key, out value);
                if (canAquire)
                    _store.Add(key, 1);
                return canAquire;
            }

            public bool TryAquire(string key, TimeSpan howLong)
            {
                return TryAquire(key);
            }

            public void Release(string key)
            {
                _store.Remove(key);
            }
        }
    internal class Monitoring : IMessageMonitor, IMeasureHandlerExecutionTime
        {
            public void HandleException(string messageType) { }
            public void HandleTime(long handleTimeMs) { }
            public void IssuePublishingMessage() { }
            public void IncrementThrottlingStatistic() { }
            public void HandleThrottlingTime(long handleTimeMs) { }
            public void PublishMessageTime(long handleTimeMs) { }
            public void ReceiveMessageTime(long handleTimeMs) { }
            public void HandlerExecutionTime(string typeName, string eventName, TimeSpan executionTime) { }
        }
    
}