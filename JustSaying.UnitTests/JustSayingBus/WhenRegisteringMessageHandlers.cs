using System;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringMessageHandlers : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber;
        private IAsyncHandler<Message> _handler1;
        private IAsyncHandler<Message2> _handler2;
        private string _region;
        private readonly Func<IAsyncHandler<Message>> _futureHandler1;
        private readonly Func<IAsyncHandler<Message2>> _futureHandler2;

        public WhenRegisteringMessageHandlers()
        {
            _futureHandler1 = () => _handler1;
            _futureHandler2 = () => _handler2;
        }

        protected override void Given()
        {
            base.Given();
            _subscriber = Substitute.For<INotificationSubscriber>();
            _handler1 = Substitute.For<IAsyncHandler<Message>>();
            _handler2 = Substitute.For<IAsyncHandler<Message2>>();
            _region = "west-1";
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationSubscriber(_region, _subscriber);
            SystemUnderTest.AddNotificationSubscriber(_region, _subscriber);
            SystemUnderTest.AddMessageHandler(_region, _subscriber.Queue, _futureHandler1);
            SystemUnderTest.AddMessageHandler(_region, _subscriber.Queue, _futureHandler2);
            SystemUnderTest.Start();
        }

        [Then]
        public void HandlersAreAdded()
        {
            _subscriber.Received().AddMessageHandler(_futureHandler1);
            _subscriber.Received().AddMessageHandler(_futureHandler2);
        }

        [Then]
        public void HandlersAreAddedBeforeSubscriberStartup()
        {
            Received.InOrder(() =>
                {
                    _subscriber.AddMessageHandler(Arg.Any<Func<IAsyncHandler<Message>>>());
                    _subscriber.AddMessageHandler(Arg.Any<Func<IAsyncHandler<Message2>>>());
                    _subscriber.Listen();
                });
        }

        public class Message2 : Message { }
    }
}