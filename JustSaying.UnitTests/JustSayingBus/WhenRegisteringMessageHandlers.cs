using System;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using NSubstitute.Experimental;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringMessageHandlers : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber;
        private IHandler<Message> _handler1;
        private IHandler<Message2> _handler2;
        private string _topic;
        private string _topic2;
        private Func<IHandler<Message>> _futureHandler1;
        private Func<IHandler<Message2>> _futureHandler2;

        public WhenRegisteringMessageHandlers()
        {
            _futureHandler1 = () => _handler1;
            _futureHandler2 = () => _handler2;
        }

        protected override void Given()
        {
            base.Given();
            _subscriber = Substitute.For<INotificationSubscriber>();
            _handler1 = Substitute.For<IHandler<Message>>();
            _handler2 = Substitute.For<IHandler<Message2>>();
            _topic = "message"; //same as message name
            _topic2 = "message2"; //same as message name
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(_topic, _subscriber);
            SystemUnderTest.AddNotificationTopicSubscriber(_topic2, _subscriber);
            SystemUnderTest.AddMessageHandler(_futureHandler1);
            SystemUnderTest.AddMessageHandler(_futureHandler2);
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
                                     _subscriber.AddMessageHandler(Arg.Any<Func<IHandler<Message>>>());
                                     _subscriber.AddMessageHandler(Arg.Any<Func<IHandler<Message2>>>());
                                     _subscriber.Listen();
                                     _subscriber.Listen();
                                 });
        }

        public class Message2 : Message { }
    }
}