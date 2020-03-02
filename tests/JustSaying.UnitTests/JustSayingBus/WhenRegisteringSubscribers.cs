using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NSubstitute;
using Shouldly;
using Xunit;
using Message = Amazon.SQS.Model.Message;

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
            _queue1
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Message>
                {
                    new TestMessage(),
                });

            _queue2 = Substitute.For<ISqsQueue>();
            _queue2.QueueName.Returns("queue2");
            _queue2
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Message>
                {
                    new TestMessage(),
                });
        }

        protected override Task WhenAsync()
        {
            SystemUnderTest.AddMessageHandler(() => new InspectableHandler<OrderAccepted>());
            SystemUnderTest.AddMessageHandler(() => new InspectableHandler<OrderRejected>());
            SystemUnderTest.AddMessageHandler(() => new InspectableHandler<SimpleMessage>());

            SystemUnderTest.AddQueue("region1", _queue1);
            SystemUnderTest.AddQueue("region1", _queue2);
            SystemUnderTest.Start();

            return Task.CompletedTask;
        }

        [Fact]
        public void SubscribersStartedUp()
        {
            _queue1.Received().GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), default);
            _queue2.Received().GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), default);
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

        private class TestMessage : Message
        {
        }
    }
}
