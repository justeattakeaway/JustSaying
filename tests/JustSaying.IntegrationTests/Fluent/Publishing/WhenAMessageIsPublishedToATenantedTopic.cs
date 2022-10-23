using JustSaying.Extensions;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToATenantedTopic
    : IntegrationTestBase
{
    public WhenAMessageIsPublishedToATenantedTopic(ITestOutputHelper outputHelper)
        : base(outputHelper)
    { }

    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var testId = Guid.NewGuid().ToString("n");

        var topicNameTemplate = "{tenant}-tenanted-topic".TruncateTo(60);

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder
                .Publications(pub => pub.WithTopic<SimpleMessage>(c =>
                    c.WithTopicName(msg => topicNameTemplate.Replace("{tenant}", msg.Tenant))))
                .Subscriptions(sub =>
                    sub.ForTopic<SimpleMessage>(c => c.WithTopicName("uk-tenanted-topic").WithQueueName($"uk-queue-{testId}"))
                        .ForTopic<SimpleMessage>(c => c.WithTopicName("it-tenanted-topic").WithQueueName($"it-queue-{testId}"))
                        .ForTopic<SimpleMessage>(c => c.WithTopicName("es-tenanted-topic").WithQueueName($"es-queue-{testId}")))
            )
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        Message CreateMessage(string tenant) => new SimpleMessage()
        {
            Content = testId,
            Tenant = tenant
        };

        string json = string.Empty;

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(CreateMessage("uk"), cancellationToken);
                await publisher.PublishAsync(CreateMessage("uk"), cancellationToken);
                await publisher.PublishAsync(CreateMessage("es"), cancellationToken);
                await publisher.PublishAsync(CreateMessage("es"), cancellationToken);
                await publisher.PublishAsync(CreateMessage("it"), cancellationToken);
                await publisher.PublishAsync(CreateMessage("it"), cancellationToken);

                var publisherJson = JsonConvert.SerializeObject(publisher.Interrogate(), Formatting.Indented);

                json = publisherJson.Replace(UniqueName, "integrationTestQueueName", StringComparison.Ordinal);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        var received = handler.ReceivedMessages;
                        received.ShouldContain(x => x.Content == testId && x.Tenant == "uk", 2);
                        received.ShouldContain(x => x.Content == testId && x.Tenant == "it", 2);
                        received.ShouldContain(x => x.Content == testId && x.Tenant == "es", 2);
                    });
            });

        json.ShouldMatchApproved(opt => opt.SubFolder("Approvals"));

    }
}
