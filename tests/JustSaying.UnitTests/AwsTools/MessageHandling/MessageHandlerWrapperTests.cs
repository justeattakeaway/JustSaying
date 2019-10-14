using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling
{
    public class MessageHandlerWrapperTests
    {
        [Fact]
        public void WrapperReturnsAFunction()
        {
            var messageLock = Substitute.For<IMessageLockAsync>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor(), NullLoggerFactory.Instance);

            var wrapped = handlerWrapper.WrapMessageHandler(() => new UnadornedHandlerAsync());

            wrapped.ShouldNotBeNull();
        }

        [Fact]
        public async Task ReturnedFunctionIsCallable()
        {
            // arrange
            var messageLock = Substitute.For<IMessageLockAsync>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor(), NullLoggerFactory.Instance);

            var mockHandler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            mockHandler.Handle(Arg.Any<SimpleMessage>()).Returns(true);

            // act
            var wrapped = handlerWrapper.WrapMessageHandler(() => mockHandler);

            var result = await wrapped(new SimpleMessage());

            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ReturnedFunctionCallsInner()
        {
            // arrange
            var messageLock = Substitute.For<IMessageLockAsync>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor(), NullLoggerFactory.Instance);

            var mockHandler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            mockHandler.Handle(Arg.Any<SimpleMessage>()).Returns(true);

            var testMessage = new SimpleMessage();

            // act
            var wrapped = handlerWrapper.WrapMessageHandler(() => mockHandler);

            await wrapped(testMessage);

            await mockHandler.Received().Handle(testMessage);
        }
    }
}
