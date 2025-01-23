using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToATopicAddressWithACustomTopicAddress(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var topicArn = await GivenAnExistingTopic("uk-simple-message", cancellationToken);
        _ = await GivenAnExistingTopic("us-simple-message", cancellationToken);
        var topicArnTemplate = topicArn.Replace("uk", "{Tenant}");
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder
                    .Publications((options) =>
                        options.WithTopicArn<SimpleMessage>(topicArnTemplate, (opt) => opt.WithTopicAddress(TenantTopicAddressCustomizer)))
                    .Subscriptions((options) =>
                    {
                        options.ForTopic<SimpleMessage>("uk-simple-message",
                            subscriptionBuilder => { subscriptionBuilder.WithQueueName(UniqueName); });
                        options.ForTopic<SimpleMessage>("us-simple-message",
                            subscriptionBuilder => { subscriptionBuilder.WithQueueName(UniqueName); });
                    }))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        string content = Guid.NewGuid().ToString();

        var ukMessage = new SimpleMessage()
        {
            Content = content,
            Tenant = "uk",
        };

        var usMessage = new SimpleMessage()
        {
            Content = content,
            Tenant = "us",
        };

        string json = "";

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                var listenerJson = JsonConvert.SerializeObject(listener.Interrogate(), Formatting.Indented);
                var publisherJson = JsonConvert.SerializeObject(publisher.Interrogate(), Formatting.Indented);

                await publisher.PublishAsync(ukMessage, cancellationToken);
                await publisher.PublishAsync(usMessage, cancellationToken);

                json = string.Join($"{Environment.NewLine}{Environment.NewLine}",
                        listenerJson,
                        publisherJson)
                    .Replace(UniqueName, "integrationTestQueueName", StringComparison.Ordinal);

                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.ReceivedMessages.Any(x => x.Content == content && x.Tenant == "uk").ShouldBeTrue();
                        handler.ReceivedMessages.Any(x => x.Content == content && x.Tenant == "us").ShouldBeTrue();
                    });
            });

        json.ShouldMatchApproved(opt => opt.SubFolder("Approvals"));
    }

    private static string TenantTopicAddressCustomizer(string topicArnTemplate, Message message) =>
        topicArnTemplate.Replace("{Tenant}", message.Tenant);
}
