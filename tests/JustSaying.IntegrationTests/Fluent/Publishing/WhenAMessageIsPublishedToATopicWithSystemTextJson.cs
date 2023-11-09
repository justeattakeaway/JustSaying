using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToATopicWithSystemTextJson(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying(
                (builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
            .AddSingleton<IMessageSerializationFactory, SystemTextJsonSerializationFactory>()
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        string content = Guid.NewGuid().ToString();

        var message = new SimpleMessage()
        {
            Content = content
        };

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(message, cancellationToken);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                    });
            });
    }
}