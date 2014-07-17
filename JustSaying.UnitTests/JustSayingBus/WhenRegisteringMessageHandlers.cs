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
        private IHandler<Message> _handler2;
        private string _topic;

        protected override void Given()
        {
            base.Given();
            _subscriber = Substitute.For<INotificationSubscriber>();
            _handler1 = Substitute.For<IHandler<Message>>();
            _handler2 = Substitute.For<IHandler<Message>>();
            _topic = "message"; //same as message name
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(_topic, _subscriber);
            SystemUnderTest.AddMessageHandler(_handler1);
            SystemUnderTest.AddMessageHandler(_handler2);
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