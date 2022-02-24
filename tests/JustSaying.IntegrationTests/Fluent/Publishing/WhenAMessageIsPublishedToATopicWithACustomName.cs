using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToATopicWithACustomName : IntegrationTestBase
{
    public WhenAMessageIsPublishedToATopicWithACustomName(ITestOutputHelper outputHelper)
        : base(outputHelper)
    { }

    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder
                    .Publications((options) =>
                        options.WithTopic<SimpleMessage>(configure => { configure.WithName("my-special-topic"); }))
                    .Subscriptions((options) =>
                        options.ForTopic<SimpleMessage>("my-special-topic",
                            subscriptionBuilder => { subscriptionBuilder.WithName(UniqueName); })))
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
