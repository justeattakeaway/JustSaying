using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenAHandlerUsesMessageContext : IntegrationTestBase
    {
        public WhenAHandlerUsesMessageContext(ITestOutputHelper outputHelper) :
            base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Message_Is_Handled()
        {
            // Arrange
            var future = new Future<SimpleMessage>();
            var accessor = new RecordingMessageContextAccessor(new MessageContextAccessor());

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) =>
                    builder.Publications((options) => options.WithQueue<SimpleMessage>(UniqueName)))
                .ConfigureJustSaying((builder) =>
                    builder.Subscriptions((options) => options.ForQueue<SimpleMessage>(UniqueName)))
                .ConfigureJustSaying((builder) =>
                    builder.Services((options) => options.WithMessageContextAccessor(() => accessor)))

                .AddSingleton(future)
                .AddSingleton<IMessageContextAccessor>(accessor)
                .AddJustSayingHandler<SimpleMessage, HandlerWithMessageContext>();

            string content = Guid.NewGuid().ToString();

            var message = new SimpleMessage
            {
                Content = content
            };

            await WhenAsync(
                services,
                async (publisher, listener, cancellationToken) =>
                {
                    listener.Start(cancellationToken);

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);

                    // Assert
                    await future.DoneSignal;

                    accessor.ValuesWritten.Count.ShouldBeGreaterThan(1);
                });
        }
    }
}
