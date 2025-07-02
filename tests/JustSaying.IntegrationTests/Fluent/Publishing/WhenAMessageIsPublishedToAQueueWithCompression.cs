using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToAQueueWithCompression(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueueAndPublicationOptions<SimpleMessage>(UniqueName,
                c =>
                {
                    c.WithWriteConfiguration(writeConfiguration =>
                    {
                        writeConfiguration.CompressionOptions = new PublishCompressionOptions
                        {
                            CompressionEncoding = ContentEncodings.GzipBase64,
                            MessageLengthThreshold = 100
                        };
                    });
                }))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        var message = new SimpleMessage
        {
            // Content longer than 100 bytes
            Content = new string('a', 500)
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
                        handler.ReceivedMessages.ShouldNotBeNull();
                        handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(message.Content);
                    });
            });
    }
}
