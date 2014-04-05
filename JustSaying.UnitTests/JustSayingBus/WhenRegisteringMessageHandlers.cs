using JustEat.Testing;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;
using NSubstitute;
using NSubstitute.Experimental;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringMessageHandlers : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber;
        private IHandler<Message> _handler1;
        private IHandler<Message> _handler2;

        protected override void Given()
        {
            base.Given();
            _subscriber = Substitute.For<INotificationSubscriber>();
            _handler1 = Substitute.For<IHandler<Message>>();
            _handler2 = Substitute.For<IHandler<Message>>();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber("OrderDispatch", _subscriber);
            SystemUnderTest.AddMessageHandler("OrderDispatch", _handler1);
            SystemUnderTest.AddMessageHandler("OrderDispatch", _handler2);
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