using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
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
        private ISqsQueue _queue1;
        private ISqsQueue _queue2;

        protected override void Given()
        {
            base.Given();
            _queue1 = Substitute.For<ISqsQueue>();
            _queue1.QueueName.Returns("queue1");
            //_subscriber1.Subscribers.Returns(new Collection<ISubscriber>
            //{
            //    new Subscriber(typeof (OrderAccepted)),
            //    new Subscriber(typeof (OrderRejected))
            //});
            _queue2 = Substitute.For<ISqsQueue>();
            _queue2.QueueName.Returns("queue2");
            // _subscriber2.Subscribers.Returns(new Collection<ISubscriber> { new Subscriber(typeof(SimpleMessage)) });
        }

        protected override Task WhenAsync()
        {
            SystemUnderTest.AddQueue("region1", _queue1);
            SystemUnderTest.AddQueue("region1", _queue2);
            SystemUnderTest.Start();

            return Task.CompletedTask;
        }

        [Fact]
        public void SubscribersStartedUp()
        {
            _queue1.Received().GetMessages(Arg.Any<int>(), Arg.Any<List<string>>(), default);
            _queue2.Received().GetMessages(Arg.Any<int>(), Arg.Any<List<string>>(), default);
        }

        // todo: how can we check this?
        //[Fact]
        //public void CallingStartTwiceDoesNotStartListeningTwice()
        //{
        //    _subscriber1.IsListening.Returns(true);
        //    _subscriber2.IsListening.Returns(true);
        //    SystemUnderTest.Start();
        //    _subscriber1.Received(1).Listen(default);
        //    _subscriber2.Received(1).Listen(default);
        //}

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
