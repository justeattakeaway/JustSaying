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
                                (config) => config.WithErrorHandler(ErrorHandler)))))
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
                    handledException.ShouldBeTrue();
                });
        }
    }
}
