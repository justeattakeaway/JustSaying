using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Middleware;

public class StopwatchMiddlewareTests
{
    private InspectableHandler<OrderAccepted> _handler;
    private TrackingLoggingMonitor _monitor;
    private MiddlewareBase<HandleMessageContext, bool> _middleware;

    [Before(Test)]
    public void Setup()
    {
        var outputHelper = TestContext.Current!.OutputWriter;
        var loggerFactory = LoggerFactory.Create(lf => lf.AddTextWriter(outputHelper).SetMinimumLevel(LogLevel.Information));

        _handler = new InspectableHandler<OrderAccepted>();
        _monitor = new TrackingLoggingMonitor(loggerFactory.CreateLogger<TrackingLoggingMonitor>());
        var serviceResolver = new InMemoryServiceResolver(c =>
            c.AddSingleton<IHandlerAsync<OrderAccepted>>(_handler)
                .AddSingleton<IMessageMonitor>(_monitor));

        _middleware = new HandlerMiddlewareBuilder(serviceResolver, serviceResolver)
            .UseHandler<OrderAccepted>()
            .UseStopwatch(_handler.GetType())
            .Build();
    }

    [Test]
    public async Task WhenMiddlewareIsWrappedinStopWatch_InnerMiddlewareIsCalled()
    {
        var context = TestHandleContexts.From<OrderAccepted>();
        var result = await _middleware.RunAsync(context, null, CancellationToken.None);

        result.ShouldBeTrue();

        _handler.ReceivedMessages.ShouldHaveSingleItem().ShouldBeOfType<OrderAccepted>();
    }

    [Test]
    public async Task WhenMiddlewareIsWrappedinStopWatch_MonitoringIsCalled()
    {
        var context = TestHandleContexts.From<OrderAccepted>();

        var result = await _middleware.RunAsync(context, null, CancellationToken.None);

        result.ShouldBeTrue();

        var handled = _monitor.HandlerExecutionTimes.ShouldHaveSingleItem();
        handled.duration.ShouldBeGreaterThan(TimeSpan.Zero);
        handled.handlerType.ShouldBe(typeof(InspectableHandler<OrderAccepted>));
        handled.messageType.ShouldBe(typeof(OrderAccepted));
    }
}
