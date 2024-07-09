using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedWithCompression(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackTopicAndPublicationOptions<SimpleMessage>(UniqueName,
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
            .AddSingleton<IMessageSerializationFactory, SystemTextJsonSerializationFactory>()
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        var message = new SimpleMessage
        {
            // Content longer than 100 bytes
            Content =
                """
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.
                """
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
                        handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(message.Content);
                    });
            });
    }
}
