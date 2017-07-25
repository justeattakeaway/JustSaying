using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling
{
    [TestFixture]
    public class FutureHandlerTests
    {
        [Test]
        public async Task FutureHandlerResolvesAndCallsActualHandler()
        {
            // arrange
            var ctx = Substitute.For<HandlerResolutionContext>("some-queue");
            var wrapper = Substitute.For<MessageHandlerWrapper>(Substitute.For<IMessageLock>(), Substitute.For<IMessageMonitor>());
            var handler = Substitute.For<IHandlerAsync<GenericMessage>>();
            handler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            var resolver = Substitute.For<IHandlerAndMetadataResolver>();
            resolver.ResolveHandler<GenericMessage>(Arg.Any<HandlerResolutionContextWithMessage>()).Returns(handler);
            var futureHandler = new FutureHandler<GenericMessage>(resolver, ctx, wrapper );

            // act
            var result = await futureHandler.Handle(new GenericMessage());

            Assert.True(result);
        }
        [Test]
        public async Task FutureHandlerWrapsHandler()
        {
            // arrange
            var ctx = Substitute.For<HandlerResolutionContext>("some-queue");
            var wrapper = Substitute.For<MessageHandlerWrapper>(Substitute.For<IMessageLock>(), Substitute.For<IMessageMonitor>());
            var handler = Substitute.For<IHandlerAsync<GenericMessage>>();
            handler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            var resolver = Substitute.For<IHandlerAndMetadataResolver>();
            resolver.ResolveHandler<GenericMessage>(Arg.Any<HandlerResolutionContextWithMessage>()).Returns(handler);
            var futureHandler = new FutureHandler<GenericMessage>(resolver, ctx, wrapper);

            // act
            await futureHandler.Handle(new GenericMessage());

            wrapper.Received().WrapMessageHandler(handler);

        }
    }
}
