using System.Linq;
using System.Collections.Generic;
using JustEat.Testing;
using NSubstitute;
using NSubstitute.Experimental;
using NUnit.Framework;
using SimplesNotificationStack.Messaging;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.Messages;

namespace Stack.UnitTests.NotificationStack
{
    public abstract class NotificationStackBaseTest : BehaviourTest<SimplesNotificationStack.Stack.NotificationStack>
    {
        protected override SimplesNotificationStack.Stack.NotificationStack CreateSystemUnderTest()
        {
            return new SimplesNotificationStack.Stack.NotificationStack(Component.BoxHandler);
        }
    }

    public class WhenRegisteringSubscribers : NotificationStackBaseTest
    {
        private IMessageSubscriber _subscriber1;
        private IMessageSubscriber _subscriber2;

        protected override void Given()
        {
            _subscriber1 = Substitute.For<IMessageSubscriber>();
            _subscriber2 = Substitute.For<IMessageSubscriber>();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, _subscriber1);
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.CustomerCommunication, _subscriber2);
            SystemUnderTest.Start();
        }

        [Then]
        public void SubscribersStartedUp()
        {
            _subscriber1.Received().Listen();
            _subscriber2.Received().Listen();
        }
    }

    public class WhenRegisteringMessageHandlers : NotificationStackBaseTest
    {
        private IMessageSubscriber _subscriber;
        private IHandler<Message> _handler1;
        private IHandler<Message> _handler2;

        protected override void Given()
        {
            _subscriber = Substitute.For<IMessageSubscriber>();
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
