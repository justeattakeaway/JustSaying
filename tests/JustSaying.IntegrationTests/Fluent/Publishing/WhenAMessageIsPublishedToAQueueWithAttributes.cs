using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent.Subscribing.Newtonsoft;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
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
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueueAndPublicationOptions<SimpleMessage>(UniqueName,
                c =>
                {
                    c.WithWriteConfiguration((SqsWriteConfiguration writeConfiguration) =>
                    {
                        writeConfiguration.QueueName = UniqueName;
                    });
                }))
            .AddSingleton<IMessageSerializationFactory, SystemTextJsonSerializationFactory>()
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        var message = new SimpleMessage
        {
            // Content longer than 100 bytes
            Content = Guid.NewGuid().ToString()
        };

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);
                var publishMetadata = new PublishMetadata();
                publishMetadata.AddMessageAttribute("Hello", "World");

                // Act
                await publisher.PublishAsync(message, publishMetadata, cancellationToken);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.HandledMessages.ShouldHaveSingleItem().message.Content.ShouldBe(message.Content);
                        handler.HandledMessages.ShouldHaveSingleItem().context.MessageAttributes.GetKeys().ShouldContain("Hello");
                    });
            });
    }

}
