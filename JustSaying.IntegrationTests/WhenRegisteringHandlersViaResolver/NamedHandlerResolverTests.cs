using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    [TestFixture]
    public class NamedHandlerResolverTests
    {
        private readonly IHandlerResolver _handlerResolver = new StructureMapNamedHandlerResolver();

        [Test]
        public void TestQueueAResolution()
        {
            var context = new HandlerResolutionContext("QueueA");
            var handler = _handlerResolver.ResolveHandler<TestMessage>(context);

            Assert.That(handler, Is.Not.Null);
            Assert.That(handler, Is.InstanceOf<HandlerA>());
        }

        [Test]
        public void TestQueueBResolution()
        {
            var context = new HandlerResolutionContext("QueueB");
            var handler = _handlerResolver.ResolveHandler<TestMessage>(context);

            Assert.That(handler, Is.Not.Null);
            Assert.That(handler, Is.InstanceOf<HandlerB>());
        }

        [Test]
        public void TestOtherQueueNameResolution()
        {
            var context = new HandlerResolutionContext("QueueWithAnyOtherName");
            var handler = _handlerResolver.ResolveHandler<TestMessage>(context);

            Assert.That(handler, Is.Not.Null);
            Assert.That(handler, Is.InstanceOf<HandlerC>());
        }

    }
}
