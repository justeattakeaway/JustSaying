using System;
using System.Collections.Generic;
using System.Threading;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.Tests.MessageStubs;
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
                .ConfigurePublisherWith(_ => { })
                .WithSnsMessagePublisher<GenericMessage>(TopicName);

            var bus = CreateMeABus.InRegion(region)
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber(TopicName)
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

    public class MessageLockStore : IMessageLock
    {
        private readonly Dictionary<string, int> _store = new Dictionary<string, int>();
        public bool TryAquire(string key)
        {
            int value;
            bool canAquire = !_store.TryGetValue(key, out value);
            if(canAquire)
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
}