using JustSaying.Extensions;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NSubstitute;

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
                    sub.ForTopic<SimpleMessage>(c => c.WithTopicName("uk-tenanted-topic").WithQueueName("uk-queue"))
                        .ForTopic<SimpleMessage>(c => c.WithTopicName("it-tenanted-topic").WithQueueName("it-queue"))
                        .ForTopic<SimpleMessage>(c => c.WithTopicName("es-tenanted-topic").WithQueueName("es-queue")))
            )
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        Message CreateMessage(string tenant) => new SimpleMessage()
        {
            Content = testId,
            Tenant = tenant
        };

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);


                // Act
                await publisher.PublishAsync(CreateMessage("uk"), cancellationToken);
                await publisher.PublishAsync(CreateMessage("es"), cancellationToken);
                await publisher.PublishAsync(CreateMessage("it"), cancellationToken);

                OutputHelper.WriteLine(JsonConvert.SerializeObject(publisher.Interrogate()));
                OutputHelper.WriteLine(JsonConvert.SerializeObject(listener.Interrogate()));


                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        var received = handler.ReceivedMessages;
                        received.ShouldContain(x => x.Content == testId && x.Tenant == "uk");
                        received.ShouldContain(x => x.Content == testId && x.Tenant == "it");
                        received.ShouldContain(x => x.Content == testId && x.Tenant == "es");
                    });
            });
    }
}
