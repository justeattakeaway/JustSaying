using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.Fluent;
using JustSaying.IntegrationTests.Fluent.Subscribing;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            var monitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<TrackingLoggingMonitor>());

            var services = GivenJustSaying()
                .ConfigureJustSaying(
                    (builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
                .AddSingleton(future)
                .AddSingleton<IMessageMonitor>(monitor)
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

                    monitor.HandledTimes.ShouldHaveSingleItem().ShouldBeGreaterThan(TimeSpan.Zero);
                    monitor.PublishMessageTimes.ShouldHaveSingleItem().ShouldBeGreaterThan(TimeSpan.Zero);
                    monitor.ReceiveMessageTimes.ShouldHaveSingleItem().duration
                        .ShouldBeGreaterThan(TimeSpan.Zero);
                });
        }
    }
}
