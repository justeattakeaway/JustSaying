using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToATopicAddress(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var topicArn = await GivenAnExistingTopic("simple-message", cancellationToken);
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder
                    .Publications((options) =>
                        options.WithTopicArn<SimpleMessage>(topicArn))
                    .Subscriptions((options) =>
                        options.ForTopic<SimpleMessage>("simple-message",
                            subscriptionBuilder => { subscriptionBuilder.WithQueueName(UniqueName); })))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        string content = Guid.NewGuid().ToString();

        var message = new SimpleMessage()
        {
            Content = content
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

                await publisher.PublishAsync(message, cancellationToken);

                json = string.Join($"{Environment.NewLine}{Environment.NewLine}",
                        listenerJson,
                        publisherJson)
                    .Replace(UniqueName, "integrationTestQueueName", StringComparison.Ordinal);

                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                        handler.ReceivedMessages.Any(x => x.Content == content).ShouldBeTrue());
            });

        json.ShouldMatchApproved(opt => opt.SubFolder("Approvals"));
    }
}
