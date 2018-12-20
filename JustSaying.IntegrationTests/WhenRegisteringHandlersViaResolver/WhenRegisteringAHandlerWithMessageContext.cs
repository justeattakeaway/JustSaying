using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Shouldly;
using StructureMap;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenRegisteringAHandlerWithMessageContext : GivenAPublisher
    {
        private Future<OrderPlaced> _handlerFuture;

        private readonly RecordingMessageContextAccessor _accessor =
            new RecordingMessageContextAccessor(new MessageContextAccessor());

        protected override Task Given()
        {
            var container = new Container(
                x => x.AddRegistry(new HandlerWithMessageContextRegistry(_accessor)));
            var resolutionContext = new HandlerResolutionContext("test");

            var handlerResolver = new StructureMapHandlerResolver(container);
            var handler = handlerResolver.ResolveHandler<OrderPlaced>(resolutionContext);
            handler.ShouldNotBeNull();

            _handlerFuture = ((HandlerWithMessageContext)handler).Future;
            DoneSignal = _handlerFuture.DoneSignal;

            var fixture = new JustSayingFixture();

            Subscriber = fixture.Builder()
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            SubscriberCts = new CancellationTokenSource();
            Subscriber.StartListening(SubscriberCts.Token);
            
            return Task.CompletedTask;
        }

        [AwsFact]
        public void ThenHandlerWillReceiveTheMessage()
        {
            _handlerFuture.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }

        [AwsFact]
        public void ThenAccessorWillWriteContext()
        {
            _accessor.ValuesWritten.Count.ShouldBeGreaterThan(0);

            var ctx = _accessor.ValuesWritten.First();
            ctx.Message.ShouldNotBeNull();
            ctx.QueueUri.ShouldNotBeNull();
        }
    }
}
