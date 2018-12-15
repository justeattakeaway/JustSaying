using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenAHandlerThrowsAnExceptionWithAMonitor : IntegrationTestBase
    {
        public WhenAHandlerThrowsAnExceptionWithAMonitor(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Message_Is_Handled()
        {
            // Arrange
            var handler = new ThrowingHandler();
            var monitoring = Substitute.For<IMessageMonitor>();

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.Publications((options) => options.WithQueue<SimpleMessage>(UniqueName)))
                .ConfigureJustSaying((builder) => builder.Subscriptions((options) => options.ForQueue<SimpleMessage>(UniqueName)))
                .ConfigureJustSaying((builder) => builder.Services((options) => options.WithMessageMonitoring(() => monitoring)))
                .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

            var message = new SimpleMessage();

            await WhenAsync(
                services,
                async (publisher, listener, cancellationToken) =>
                {
                    listener.Start(cancellationToken);

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);
                    await handler.DoneSignal.Task;

                    // Assert
                    await handler.Received().Handle(Arg.Any<SimpleMessage>());
                    handler.MessageReceived.ShouldNotBeNull();

                    monitoring.Received().HandleException(Arg.Any<Type>());
                });
        }
    }
}
