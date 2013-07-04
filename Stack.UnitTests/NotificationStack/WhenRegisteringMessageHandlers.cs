using System;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Testing;
using NSubstitute;
using NSubstitute.Experimental;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenRegisteringMessageHandlers : NotificationStackBaseTest
    {
        private INotificationSubscriber _subscriber;
        private IHandler<Message> _handler1;
        private IHandler<Message> _handler2;

        protected override void Given()
        {
            _subscriber = Substitute.For<INotificationSubscriber>();
            _handler1 = Substitute.For<IHandler<Message>>();
            _handler2 = Substitute.For<IHandler<Message>>();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, _subscriber);
            SystemUnderTest.AddMessageHandler(NotificationTopic.OrderDispatch, _handler1);
            SystemUnderTest.AddMessageHandler(NotificationTopic.OrderDispatch, _handler2);
            SystemUnderTest.Start();
        }

        [Then]
        public void HandlersAreAdded()
        {
            _subscriber.Received().AddMessageHandler(_handler1);
            _subscriber.Received().AddMessageHandler(_handler2);
        }

        [Then]
        public void HandlersAreAddedBeforeSubscriberStartup()
        {
            Received.InOrder(() =>
                                 {
                                     _subscriber.AddMessageHandler(Arg.Any<IHandler<Message>>());
                                     _subscriber.AddMessageHandler(Arg.Any<IHandler<Message>>());
                                     _subscriber.Listen();
                                 });
        }
    }
}