using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
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
            var messageLock = Substitute.For<IMessageLock>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());

            var wrapped = handlerWrapper.WrapMessageHandler(() => new UnadornedHandlerAsync());

            wrapped.ShouldNotBeNull();
        }

        [Fact]
        public async Task ReturnedFunctionIsCallable()
        {
            // arrange
            var messageLock = Substitute.For<IMessageLock>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());

            var mockHandler = Substitute.For<IHandlerAsync<GenericMessage>>();
            mockHandler.Handle(Arg.Any<GenericMessage>()).Returns(Task.FromResult(true));

            // act
             var wrapped = handlerWrapper.WrapMessageHandler(() => mockHandler);

            var result = await wrapped(new GenericMessage());

            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ReturnedFunctionCallsInner()
        {
            // arrange
            var messageLock = Substitute.For<IMessageLock>();
            var handlerWrapper = new MessageHandlerWrapper(messageLock, new NullOpMessageMonitor());

            var mockHandler = Substitute.For<IHandlerAsync<GenericMessage>>();
            mockHandler.Handle(Arg.Any<GenericMessage>()).Returns(Task.FromResult(true));

            var testMessage = new GenericMessage();

            // act
            var wrapped = handlerWrapper.WrapMessageHandler(() => mockHandler);

            await wrapped(testMessage);

            await mockHandler.Received().Handle(testMessage);
        }
    }
}
