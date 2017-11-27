using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

// we use the obsolete interface"IHandler<T>" here
#pragma warning disable 618

namespace JustSaying.UnitTests.Messaging.MessageHandling
{
    public class BlockingHandlerTests
    {
        [Fact]
        public void WhenInnerIsNull_ExcpetionIsThrown()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new Action(() => new BlockingHandler<OrderAccepted>(null)).ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public async Task WhenAMessageIsHandled_TheInnerIsCalled()
        {
            var inner = Substitute.For<IHandler<OrderAccepted>>();
            inner.Handle(Arg.Any<OrderAccepted>())
                .Returns(false);

            var handler = new BlockingHandler<OrderAccepted>(inner);

            var message = new OrderAccepted();

            await handler.Handle(message);

            inner.Received().Handle(message);
        }

        [Fact]
        public async Task WhenAMessageIsHandled_TheInnerResultFalseIsReturned()
        {
            var inner = Substitute.For<IHandler<OrderAccepted>>();
            inner.Handle(Arg.Any<OrderAccepted>())
                .Returns(false);

            var handler = new BlockingHandler<OrderAccepted>(inner);

            var message = new OrderAccepted();

            var result = await handler.Handle(message);

            result.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenAMessageIsHandled_TheInnerResultTrueIsReturned()
        {
            var inner = Substitute.For<IHandler<OrderAccepted>>();
            inner.Handle(Arg.Any<OrderAccepted>())
                .Returns(true);

            var handler = new BlockingHandler<OrderAccepted>(inner);

            var message = new OrderAccepted();

            var result = await handler.Handle(message);

            result.ShouldBeTrue();
        }
    }
}
