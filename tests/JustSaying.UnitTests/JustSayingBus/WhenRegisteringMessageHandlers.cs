using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.UnitTests.AwsTools.MessageHandling;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;


namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringMessageHandlers : GivenAServiceBus
    {
        private ISqsQueue _queue;
        private IHandlerAsync<Message> _handler1;
        private IHandlerAsync<Message2> _handler2;
        private HandleMessageMiddleware _futureHandler1;
        private HandleMessageMiddleware _futureHandler2;

        protected override void Given()
        {
            base.Given();

            _handler1 = Substitute.For<IHandlerAsync<Message>>();
            _handler2 = Substitute.For<IHandlerAsync<Message2>>();

            _futureHandler1 = new DelegateMessageHandlingMiddleware<Message>(m => _handler1.Handle(m));
            _futureHandler2 = new DelegateMessageHandlingMiddleware<Message2>(m => _handler2.Handle(m));
            _queue = Substitute.For<ISqsQueue>();
        }

        protected override async Task WhenAsync()
        {
            SystemUnderTest.AddQueue(typeof(Message).FullName, _queue);
            SystemUnderTest.AddMessageMiddleware<Message>(_queue.QueueName, _futureHandler1);
            SystemUnderTest.AddMessageMiddleware<Message2>(_queue.QueueName, _futureHandler2);

            var cts = new CancellationTokenSource(TimeoutPeriod);
            await SystemUnderTest.StartAsync(cts.Token);
        }

        [Fact]
        public void HandlersAreAdded()
        {
            SystemUnderTest.MiddlewareMap.Contains(_queue.QueueName, typeof(Message)).ShouldBeTrue();
            SystemUnderTest.MiddlewareMap.Contains(_queue.QueueName, typeof(Message2)).ShouldBeTrue();
        }

        public class Message2 : Message { }

        public WhenRegisteringMessageHandlers(ITestOutputHelper outputHelper) : base(outputHelper)
        { }
    }
}
