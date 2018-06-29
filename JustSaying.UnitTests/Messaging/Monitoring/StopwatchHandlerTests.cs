using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Monitoring
{
    public class StopwatchHandlerTests
    {
        [Fact]
        public async Task WhenHandlerIsWrappedinStopWatch_InnerHandlerIsCalled()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            var result = await stopWatchHandler.Handle(new OrderAccepted());
            result.ShouldBeTrue();
            
            await handler.Received(1).Handle(Arg.Any<OrderAccepted>());
        }

        [Fact]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalled()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1). HandlerExecutionTime(
                Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<TimeSpan>());
        }

        [Fact]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalledWithCorrectTypes()
        {
            var handler = MockHandler();
            var innnerHandlerName = handler.GetType().Name.ToLower();

            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1).HandlerExecutionTime(
                handler.GetType(), typeof(OrderAccepted), Arg.Any<TimeSpan>());
        }

        [Fact]
        public async Task WhenHandlerIsWrappedinStopWatch_MonitoringIsCalledWithTiming()
        {
            var handler = MockHandler();
            var monitoring = Substitute.For<IMeasureHandlerExecutionTime>();

            var stopWatchHandler = new StopwatchHandler<OrderAccepted>(handler, monitoring);

            await stopWatchHandler.Handle(new OrderAccepted());

            monitoring.Received(1).HandlerExecutionTime(
                Arg.Any<Type>(), Arg.Any<Type>(), 
                Arg.Is<TimeSpan>(ts => ts > TimeSpan.Zero));
        }

        private static IHandlerAsync<OrderAccepted> MockHandler()
        {
            var handler = Substitute.For<IHandlerAsync<OrderAccepted>>();
            handler.Handle(Arg.Any<OrderAccepted>())
                .Returns(Task.FromResult(true));
            return handler;
        }
    }
}
