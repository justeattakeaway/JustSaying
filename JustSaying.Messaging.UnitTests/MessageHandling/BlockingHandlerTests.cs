using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.MessageHandling
{
#pragma warning disable 618
    [TestFixture]
    public class BlockingHandlerTests
    {
        [Test]
        public void WhenInnerIsNull_ExcpetionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() =>
            { new BlockingHandler<OrderAccepted>(null); });
        }

        [Test]
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

        [Test]
        public async Task WhenAMessageIsHandled_TheInnerResultFalseIsReturned()
        {
            var inner = Substitute.For<IHandler<OrderAccepted>>();
            inner.Handle(Arg.Any<OrderAccepted>())
                .Returns(false);

            var handler = new BlockingHandler<OrderAccepted>(inner);

            var message = new OrderAccepted();

            var result = await handler.Handle(message);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task WhenAMessageIsHandled_TheInnerResultTrueIsReturned()
        {
            var inner = Substitute.For<IHandler<OrderAccepted>>();
            inner.Handle(Arg.Any<OrderAccepted>())
                .Returns(true);

            var handler = new BlockingHandler<OrderAccepted>(inner);

            var message = new OrderAccepted();

            var result = await handler.Handle(message);

            Assert.That(result, Is.True);
        }
#pragma warning restore 618
    }
}
