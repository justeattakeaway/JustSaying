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
                .Subscriptions(sub => sub.ForTopic<SimpleMessage>()))
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

                OutputHelper.WriteLine(JsonConvert.SerializeObject(publisher.Interrogate()));
                OutputHelper.WriteLine(JsonConvert.SerializeObject(listener.Interrogate()));

                // Act
                await publisher.PublishAsync(message, cancellationToken);

                // Assert

                await Patiently.AssertThatAsync(OutputHelper,
                    () => handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content));
            });
    }
}
