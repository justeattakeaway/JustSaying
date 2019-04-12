using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringSubscribers : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber1;
        private INotificationSubscriber _subscriber2;

        protected override void Given()
        {
            base.Given();
            _subscriber1 = Substitute.For<INotificationSubscriber>();
            _subscriber1.Queue.Returns("queue1");
            _subscriber1.Subscribers.Returns(new Collection<ISubscriber>
            {
                new Subscriber(typeof (OrderAccepted)),
                new Subscriber(typeof (OrderRejected))
            });
            _subscriber2 = Substitute.For<INotificationSubscriber>();
            _subscriber2.Queue.Returns("queue2");
            _subscriber2.Subscribers.Returns(new Collection<ISubscriber> { new Subscriber(typeof(SimpleMessage)) });
        }

        protected override Task WhenAsync()
        {
            SystemUnderTest.AddNotificationSubscriber("region1", _subscriber1);
            SystemUnderTest.AddNotificationSubscriber("region1", _subscriber2);
            SystemUnderTest.Start();

            return Task.CompletedTask;
        }

        [Fact]
        public void SubscribersStartedUp()
        {
            _subscriber1.Received().Listen(default);
            _subscriber2.Received().Listen(default);
        }

        [Fact]
        public void CallingStartTwiceDoesNotStartListeningTwice()
        {
            _subscriber1.IsListening.Returns(true);
            _subscriber2.IsListening.Returns(true);
            SystemUnderTest.Start();
            _subscriber1.Received(1).Listen(default);
            _subscriber2.Received(1).Listen(default);
        }

        [Fact]
        public void AndInterrogationShowsPublishersHaveBeenSet()
        {
            var response = SystemUnderTest.WhatDoIHave();

            response.Subscribers.Count().ShouldBe(3);
            response.Subscribers.First(x => x.MessageType == typeof(OrderAccepted)).ShouldNotBe(null);
            response.Subscribers.First(x => x.MessageType == typeof(OrderRejected)).ShouldNotBe(null);
            response.Subscribers.First(x => x.MessageType == typeof(SimpleMessage)).ShouldNotBe(null);
        }
    }
}
