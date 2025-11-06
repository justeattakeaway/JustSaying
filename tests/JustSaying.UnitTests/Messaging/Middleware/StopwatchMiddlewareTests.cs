using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Middleware
{
    public class StopwatchMiddlewareTests
    {
        private readonly InspectableHandler<OrderAccepted> _handler;
        private readonly TrackingLoggingMonitor _monitor;
        private readonly MiddlewareBase<HandleMessageContext, bool> _middleware;

        public StopwatchMiddlewareTests(ITestOutputHelper outputHelper)
        {
            var loggerFactory = LoggerFactory.Create(lf => lf.AddXUnit(outputHelper));

            _handler = new InspectableHandler<OrderAccepted>();
            _monitor = new TrackingLoggingMonitor(loggerFactory.CreateLogger<TrackingLoggingMonitor>());
            var serviceResolver = new FakeServiceResolver(c =>
                c.AddSingleton<IHandlerAsync<OrderAccepted>>(_handler)
                    .AddSingleton<IMessageMonitor>(_monitor));

            _middleware = new HandlerMiddlewareBuilder(serviceResolver, serviceResolver)
                .UseHandler<OrderAccepted>()
                .UseStopwatch(_handler.GetType())
                .Build();
        }

        [Fact]
        public async Task WhenMiddlewareIsWrappedinStopWatch_InnerMiddlewareIsCalled()
        {
            var context = new HandleMessageContext(new OrderAccepted(), typeof(OrderAccepted), "test-queue");

            var result = await _middleware.RunAsync(context, null, CancellationToken.None);

            result.ShouldBeTrue();

            _handler.ReceivedMessages.ShouldHaveSingleItem().ShouldBeOfType<OrderAccepted>();
        }

        [Fact]
        public async Task WhenMiddlewareIsWrappedinStopWatch_MonitoringIsCalled()
        {
            var context = new HandleMessageContext(new OrderAccepted(), typeof(OrderAccepted), "test-queue");

            var result = await _middleware.RunAsync(context, null, CancellationToken.None);

            result.ShouldBeTrue();

            var handled = _monitor.HandlerExecutionTimes.ShouldHaveSingleItem();
            handled.duration.ShouldBeGreaterThan(TimeSpan.Zero);
            handled.handlerType.ShouldBe(typeof(InspectableHandler<OrderAccepted>));
            handled.messageType.ShouldBe(typeof(OrderAccepted));
        }
    }
}
