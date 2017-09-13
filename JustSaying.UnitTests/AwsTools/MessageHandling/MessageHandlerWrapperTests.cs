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
    public class MessageHandlerWrapperTests
    {
        [Test]
        public void WrapperReturnsAFunction()
        {
            var messageLock = Substitute.For<IMessageLock>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());

            var wrapped = handlerWrapper.WrapMessageHandler(() => new UnadornedHandlerAsync());

            Assert.That(wrapped, Is.Not.Null);
        }
        [Test]
        public async Task ReturnedFunctionIsCallable()
        {
            // arrange
            var messageLock = Substitute.For<IMessageLock>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());

            var mockHandler = Substitute.For<IHandlerAsync<GenericMessage>>();
            mockHandler.Handle(Arg.Any<GenericMessage>()).Returns(true);

            // act
             var wrapped = handlerWrapper.WrapMessageHandler(() => mockHandler);

            var result = await wrapped(new GenericMessage());

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ReturnedFunctionCallsInner()
        {
            // arrange
            var messageLock = Substitute.For<IMessageLock>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());

            var mockHandler = Substitute.For<IHandlerAsync<GenericMessage>>();
            mockHandler.Handle(Arg.Any<GenericMessage>()).Returns(true);

            var testMessage = new GenericMessage();

            // act
            var wrapped = handlerWrapper.WrapMessageHandler(() => mockHandler);

            await wrapped(testMessage);


            await mockHandler.Received().Handle(testMessage);
        }
    }
}
