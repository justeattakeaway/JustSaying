using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessagesAreBatchPublishedToATopicAddressWithACustomTopicAddress(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var topicArn = await GivenAnExistingTopic("at-simple-message", cancellationToken);
        _ = await GivenAnExistingTopic("us-simple-message", cancellationToken);
        var topicArnTemplate = topicArn.Replace("at", "{Tenant}");
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder
                    .Publications((options) =>
                        options.WithTopicArn<SimpleMessage>(topicArnTemplate, (opt) => opt.WithTopicAddress(TenantTopicAddressCustomizer)))
                    .Subscriptions((options) =>
                    {
                        options.ForTopic<SimpleMessage>("at-simple-message",
                            subscriptionBuilder => { subscriptionBuilder.WithQueueName(UniqueName); });
                        options.ForTopic<SimpleMessage>("us-simple-message",
                            subscriptionBuilder => { subscriptionBuilder.WithQueueName(UniqueName); });
                    }))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        string content = Guid.NewGuid().ToString();
        string secondContent = Guid.NewGuid().ToString();

        Message[] messages =[
            new SimpleMessage()
            {
                Content = content,
                Tenant = "at",
            },
            new SimpleMessage()
            {
                Content = content,
                Tenant = "us",
            },
            new SimpleMessage()
            {
                Content = secondContent,
                Tenant = "at",
            },
            new SimpleMessage()
            {
                Content = secondContent,
                Tenant = "us",
            }
        ];

        string json = "";

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                var batchPublisher = publisher.ShouldBeAssignableTo<IMessageBatchPublisher>();
                await batchPublisher.PublishAsync(messages, cancellationToken);

                // Interrogation has to come after publishing, as per-arn publishers are created on demand
                var listenerJson = JsonConvert.SerializeObject(listener.Interrogate(), Formatting.Indented);
                var publisherJson = JsonConvert.SerializeObject(publisher.Interrogate(), Formatting.Indented);

                json = string.Join($"{Environment.NewLine}{Environment.NewLine}",
                        listenerJson,
                        publisherJson)
                    .Replace(UniqueName, "integrationTestQueueName", StringComparison.Ordinal);

                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.ReceivedMessages.Any(x => x.Content == content && x.Tenant == "at").ShouldBeTrue();
                        handler.ReceivedMessages.Any(x => x.Content == secondContent && x.Tenant == "at").ShouldBeTrue();
                        handler.ReceivedMessages.Any(x => x.Content == content && x.Tenant == "us").ShouldBeTrue();
                        handler.ReceivedMessages.Any(x => x.Content == secondContent && x.Tenant == "us").ShouldBeTrue();
                        return true;
                    }, TimeSpan.FromSeconds(15));
            });

        json.ShouldMatchApproved(opt => opt.SubFolder("Approvals"));
    }

    private static string TenantTopicAddressCustomizer(string topicArnTemplate, Message message) =>
        topicArnTemplate.Replace("{Tenant}", message.Tenant);
}
