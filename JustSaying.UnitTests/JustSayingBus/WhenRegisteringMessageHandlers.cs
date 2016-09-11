using System;
using System.Threading.Tasks;
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
        private IHandlerAsync<Message> _handler1;
        private IHandlerAsync<Message2> _handler2;
        private string _region;
        private readonly Func<IHandlerAsync<Message>> _futureHandler1;
        private readonly Func<IHandlerAsync<Message2>> _futureHandler2;

        public WhenRegisteringMessageHandlers()
        {
            _futureHandler1 = () => _handler1;
            _futureHandler2 = () => _handler2;
        }

        protected override void Given()
        {
            base.Given();
            _subscriber = Substitute.For<INotificationSubscriber>();
            _handler1 = Substitute.For<IHandlerAsync<Message>>();
            _handler2 = Substitute.For<IHandlerAsync<Message2>>();
            _region = "west-1";
        }

        protected override Task When()
        {
            SystemUnderTest.AddNotificationSubscriber(_region, _subscriber);
            SystemUnderTest.AddNotificationSubscriber(_region, _subscriber);
            SystemUnderTest.AddMessageHandler(_region, _subscriber.Queue, _futureHandler1);
            SystemUnderTest.AddMessageHandler(_region, _subscriber.Queue, _futureHandler2);
            SystemUnderTest.Start();

            return Task.CompletedTask;
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
                    _subscriber.AddMessageHandler(Arg.Any<Func<IHandlerAsync<Message>>>());
                    _subscriber.AddMessageHandler(Arg.Any<Func<IHandlerAsync<Message2>>>());
                    _subscriber.Listen();
                });
        }

        public class Message2 : Message { }
    }
}
