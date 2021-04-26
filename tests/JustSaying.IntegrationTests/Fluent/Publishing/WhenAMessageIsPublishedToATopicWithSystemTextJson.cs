using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Publishing
{
    public class WhenAMessageIsPublishedToATopicWithSystemTextJson : IntegrationTestBase
    {
        public WhenAMessageIsPublishedToATopicWithSystemTextJson(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

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
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                });
        }
    }
}
