using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using Shouldly;
using StructureMap;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenRegisteringASingleHandlerViaContainer : GivenAPublisher
    {
        private Future<OrderPlaced> _handlerFuture;

        protected override void Given()
        {
           var container = new Container(x => x.AddRegistry(new SingleHandlerRegistry()));
            var resolutionContext = new HandlerResolutionContext("test");

            var handlerResolver = new StructureMapHandlerResolver(container);
            var handler = handlerResolver.ResolveHandler<OrderPlaced>(resolutionContext);
            handler.ShouldNotBeNull();

            _handlerFuture = ((OrderProcessor)handler).Future;
            DoneSignal = _handlerFuture.DoneSignal;

            var fixture = new JustSayingFixture();

            Subscriber = fixture.Builder()
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            Subscriber.StartListening();
        }

        [Fact]
        public void ThenHandlerWillReceiveTheMessage()
        {
            _handlerFuture.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}
