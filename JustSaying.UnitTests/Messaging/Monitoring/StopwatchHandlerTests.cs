using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Monitoring
{
    [TestFixture]
    public class StopwatchHandlerTests
    {
        [Test]
        public async Task WhenHandlerIsWrappedinStopWatch_InnerHandlerIsCalled()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            var result = await stopWatchHandler.Handle(new OrderAccepted());
            Assert.That(result, Is.True);

            await handler.Received(1).Handle(Arg.Any<OrderAccepted>());
        }

        [Test]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalled()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1). HandlerExecutionTime(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>());
        }

        [Test]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalledWithCorrectTypeNames()
        {
            var handler = MockHandler();
            var innnerHandlerName = handler.GetType().Name.ToLower();

            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1).HandlerExecutionTime(
                innnerHandlerName, "orderaccepted", Arg.Any<TimeSpan>());
        }

        [Test]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalledWithTiming()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1).HandlerExecutionTime(
                Arg.Any<string>(), Arg.Any<string>(), 
                Arg.Is<TimeSpan>(ts => ts > TimeSpan.Zero));
        }

        private static IHandlerAsync<OrderAccepted> MockHandler()
        {
            var handler = Substitute.For<IHandlerAsync<OrderAccepted>>();
            handler.Handle(Arg.Any<OrderAccepted>())
                .Returns(true);
            return handler;
        }
    }
}
