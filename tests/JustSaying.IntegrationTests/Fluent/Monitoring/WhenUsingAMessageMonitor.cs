using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.Fluent;
using JustSaying.IntegrationTests.Fluent.Subscribing;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.Fluent.Monitoring
{
    public class WhenUsingAMessageMonitor : IntegrationTestBase
    {
        public WhenUsingAMessageMonitor(ITestOutputHelper outputHelper) : base(outputHelper)
        { }

        [AwsFact]
        public async Task MonitorShouldBeCalled()
        {
            // Arrange
            var future = new Future<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying(
                    (builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
                .ConfigureJustSaying(
                    (builder) => builder.Services(s =>
                        s.WithMessageMonitoring(
                            builder.ServiceResolver.ResolveService<TrackingLoggingMonitor>)))
                .AddSingleton(future)
                .AddSingleton<TrackingLoggingMonitor>()
                .AddJustSayingHandler<SimpleMessage, HandlerWithMessageContext>();

            string content = Guid.NewGuid().ToString();

            var message = new SimpleMessage
            {
                Content = content
            };

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);

                    // Assert
                    await future.DoneSignal;

                    var monitor = serviceProvider.GetService<TrackingLoggingMonitor>();

                    monitor.HandledTimes.ShouldHaveSingleItem().ShouldBeGreaterThan(TimeSpan.Zero);
                    monitor.PublishMessageTimes.ShouldHaveSingleItem().ShouldBeGreaterThan(TimeSpan.Zero);
                    monitor.ReceiveMessageTimes.ShouldHaveSingleItem().duration
                        .ShouldBeGreaterThan(TimeSpan.Zero);
                });
        }
    }
}
