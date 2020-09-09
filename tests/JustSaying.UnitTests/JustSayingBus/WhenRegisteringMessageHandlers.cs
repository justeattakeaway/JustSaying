using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringMessageHandlers : GivenAServiceBus
    {
        private ISqsQueue _queue;
        private IHandlerAsync<Message> _handler1;
        private IHandlerAsync<Message2> _handler2;
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
        }

        protected override async Task WhenAsync()
        {
            SystemUnderTest.AddQueue(typeof(Message).FullName, _queue);
            SystemUnderTest.AddMessageHandler(_queue.QueueName, _futureHandler1);
            SystemUnderTest.AddMessageHandler(_queue.QueueName, _futureHandler2);

            var cts = new CancellationTokenSource(TimeoutPeriod);
            await SystemUnderTest.StartAsync(cts.Token);
        }

        [Fact]
        public void HandlersAreAdded()
        {
            SystemUnderTest.HandlerMap.Contains(_queue.QueueName, typeof(Message)).ShouldBeTrue();
            SystemUnderTest.HandlerMap.Contains(_queue.QueueName, typeof(Message2)).ShouldBeTrue();
        }

        public class Message2 : Message { }

        public WhenRegisteringMessageHandlers(ITestOutputHelper outputHelper) : base(outputHelper)
        { }
    }
}
