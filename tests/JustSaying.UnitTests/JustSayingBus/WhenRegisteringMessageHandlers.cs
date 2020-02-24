using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringMessageHandlers : GivenAServiceBus
    {
        private ISqsQueue _queue;
        private IHandlerAsync<Message> _handler1;
        private IHandlerAsync<Message2> _handler2;
        private string _region;
        private Func<IHandlerAsync<Message>> _futureHandler1;
        private Func<IHandlerAsync<Message2>> _futureHandler2;

        protected override void Given()
        {
            base.Given();
            _futureHandler1 = () => _handler1;
            _futureHandler2 = () => _handler2;
            _queue = Substitute.For<ISqsQueue>();
            _handler1 = Substitute.For<IHandlerAsync<Message>>();
            _handler2 = Substitute.For<IHandlerAsync<Message2>>();
            _region = "west-1";
        }

        protected override Task WhenAsync()
        {
            SystemUnderTest.AddQueue(_region, _queue);
            SystemUnderTest.AddMessageHandler(_region, _queue.QueueName, _futureHandler1);
            SystemUnderTest.AddMessageHandler(_region, _queue.QueueName, _futureHandler2);
            SystemUnderTest.Start();

            return Task.CompletedTask;
        }

        [Fact]
        public void HandlersAreAdded()
        {
            SystemUnderTest.HandlerMap.ContainsKey(typeof(Message)).ShouldBeTrue();
            SystemUnderTest.HandlerMap.ContainsKey(typeof(Message2)).ShouldBeTrue();
        }

        // todo: access the running bus?/check it has the correct handlers?
        //[Fact]
        //public void HandlersAreAddedBeforeSubscriberStartup()
        //{
        //    Received.InOrder(() =>
        //        {
        //            _subscriber.AddMessageHandler(Arg.Any<Func<IHandlerAsync<Message>>>());
        //            _subscriber.AddMessageHandler(Arg.Any<Func<IHandlerAsync<Message2>>>());
        //            _subscriber.Listen(default);
        //        });
        //}

        public class Message2 : Message { }
    }
}
