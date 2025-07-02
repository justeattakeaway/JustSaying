using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToAQueueWithAttribute(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    public class SimpleMessageWithStringAttributesHandler(IMessageContextAccessor contextAccessor) : IHandlerAsync<SimpleMessage>
    {
        private readonly IMessageContextAccessor _contextAccessor = contextAccessor;

        public Task<bool> Handle(SimpleMessage message)
        {
            HandledMessages.Add((_contextAccessor.MessageContext, message));
            return Task.FromResult(true);
        }

        public List<(MessageContext context, SimpleMessage message)> HandledMessages { get; } = new List<(MessageContext, SimpleMessage)>();
    }

    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var handler = new SimpleMessageWithStringAttributesHandler(new MessageContextAccessor());

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueueAndPublicationOptions<SimpleMessage>(UniqueName))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        var message = new SimpleMessage
        {
            // Content longer than 100 bytes
            Content = Guid.NewGuid().ToString()
        };
        var publishMetadata = new PublishMetadata();
        publishMetadata.AddMessageAttribute("Hello", "World");

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(message, publishMetadata, cancellationToken);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        var (actualMessageContext, actualMessage) = handler.HandledMessages.ShouldHaveSingleItem();
                        actualMessage.Content.ShouldBe(message.Content);
                        actualMessageContext.MessageAttributes.ShouldNotBeNull();
                        actualMessageContext.MessageAttributes.GetKeys().ShouldContain("Hello");
                    });
            });
    }

}
