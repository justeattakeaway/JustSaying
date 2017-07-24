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
        public void WrapperReturnsAFunction()
        {
            var messageLock = Substitute.For<IMessageLock>();
            var ctx = Substitute.For<HandlerResolutionContext>("some-queue");
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());
            var wrapped = handlerWrapper.WrapMessageHandler(new FutureHandler<GenericMessage>(new UnadornedHandlerAsync(), ctx));

            Assert.That(wrapped, Is.Not.Null);
        }
        [Test]
        public async Task ReturnedFunctionIsCallable()
        {
            // arrange
            var messageLock = Substitute.For<IMessageLock>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());

            var mockHandler = Substitute.For<IHandlerAsync<GenericMessage>>();
            mockHandler.Handle(Arg.Any<GenericMessage>()).Returns(Task.FromResult(true));

            var context = Substitute.For<HandlerResolutionContext>("some-queue");

            // act
            var wrapped = handlerWrapper.WrapMessageHandler(new FutureHandler<GenericMessage>(mockHandler, context));

            var result = await wrapped(new GenericMessage());

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ReturnedFunctionCallsInner()
        {
            // arrange
            var messageLock = Substitute.For<IMessageLock>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());

            var context = Substitute.For<HandlerResolutionContext>("some-queue");

            var mockHandler = Substitute.For<IHandlerAsync<GenericMessage>>();

            var testMessage = new GenericMessage();


            // act
            var wrapped = handlerWrapper.WrapMessageHandler(new FutureHandler<GenericMessage>(mockHandler, context));

            await wrapped(testMessage);


            await mockHandler.Received().Handle(testMessage);
        }
    }
}
