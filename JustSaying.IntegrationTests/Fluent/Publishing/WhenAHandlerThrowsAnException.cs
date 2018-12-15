using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Publishing
{
    public class WhenAHandlerThrowsAnException : IntegrationTestBase
    {
        public WhenAHandlerThrowsAnException(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Message_Is_Handled()
        {
            // Arrange
            bool handledException = false;

            void ErrorHandler(Exception exception)
            {
                handledException = true;
            }

            var handler = new ThrowingHandler();

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.Publications((options) => options.WithQueue<SimpleMessage>(UniqueName)))
                .ConfigureJustSaying(
                    (builder) => builder.Subscriptions(
                        (options) => options.ForQueue<SimpleMessage>(
                            (queue) => queue.WithName(UniqueName).WithReadConfiguration(
                                (config) => config.WithInstancePosition(1).WithErrorHandler(ErrorHandler)))))
                .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

            var message = new SimpleMessage();

            await WhenAsync(
                services,
                async (publisher, listener, cancellationToken) =>
                {
                    listener.Start(cancellationToken);

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);

                    // Assert
                    await handler.DoneSignal.Task;

                    await handler.Received().Handle(Arg.Any<SimpleMessage>());
                    handler.MessageReceived.ShouldNotBeNull();
                    handledException.ShouldBeTrue();
                });
        }
    }
}
