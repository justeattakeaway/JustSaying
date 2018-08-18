using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    [Collection(GlobalSetup.CollectionName)]
    public class NamedHandlerResolverTests
    {
        private readonly IHandlerResolver _handlerResolver = new StructureMapNamedHandlerResolver();

        [Fact]
        public void TestQueueAResolution()
        {
            var context = new HandlerResolutionContext("QueueA");
            var handler = _handlerResolver.ResolveHandler<TestMessage>(context);

            handler.ShouldNotBeNull();
            handler.ShouldBeAssignableTo<HandlerA>();
        }

        [Fact]
        public void TestQueueBResolution()
        {
            var context = new HandlerResolutionContext("QueueB");
            var handler = _handlerResolver.ResolveHandler<TestMessage>(context);

            handler.ShouldNotBeNull();
            handler.ShouldBeAssignableTo<HandlerB>();
        }

        [Fact]
        public void TestOtherQueueNameResolution()
        {
            var context = new HandlerResolutionContext("QueueWithAnyOtherName");
            var handler = _handlerResolver.ResolveHandler<TestMessage>(context);

            handler.ShouldNotBeNull();
            handler.ShouldBeAssignableTo<HandlerC>();
        }
    }
}
