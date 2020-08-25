using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenAHandlerThrowsAnExceptionWithNoMonitor : IntegrationTestBase
    {
        public WhenAHandlerThrowsAnExceptionWithNoMonitor(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Message_Is_Handled()
        {
            // Arrange
            var monitor = new TrackingMonitor();

            var handler = new ThrowingHandler();

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.Publications((options) =>
                    options.WithQueue<SimpleMessage>(UniqueName)))
                .ConfigureJustSaying(
                    (builder) => builder.Subscriptions(
                        (options) => options.ForQueue<SimpleMessage>(
                            (queue) => queue.WithName(UniqueName)))
                        .Services(c => c.WithMessageMonitoring(() => monitor)))
                .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

            var message = new SimpleMessage();

            await WhenAsync(
                services,
                async (publisher, listener, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);
                    await handler.DoneSignal.Task;

                    // Assert
                    handler.MessageReceived.ShouldNotBeNull();
                    monitor.ErrorCount.ShouldBe(1);
                });
        }
    }

    public class TrackingMonitor : IMessageMonitor
    {
        public void HandleException(Type messageType)
        {
        }

        public void HandleError(Exception ex, Message message)
        {
            ErrorCount++;
        }

        public int ErrorCount { get; private set; }

        public void HandleTime(TimeSpan duration)
        {
        }

        public void IssuePublishingMessage()
        {
        }

        public void IncrementThrottlingStatistic()
        {
        }

        public void HandleThrottlingTime(TimeSpan duration)
        {
        }

        public void PublishMessageTime(TimeSpan duration)
        {
        }

        public void ReceiveMessageTime(TimeSpan duration, string queueName, string region)
        {
        }
    }
}
