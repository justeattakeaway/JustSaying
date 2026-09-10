using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenAChannelNameAndSchemaAreShared
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class Category
    {
        public string Name { get; set; }
        public Category Parent { get; set; }
    }

    public sealed class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
    {
        public Task<bool> Handle(OrderPlaced message) => Task.FromResult(true);
    }

    private static async Task<JsonDocument> GenerateAsync(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return JsonDocument.Parse(writer.ToString());
    }

    [Test]
    public async Task ATopicAndQueueThatShareANameBecomeDistinctChannels()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));

            // Publishing and subscribing the same message type: the topic and the queue both
            // default to the message type name, so their channels must not collapse into one.
            config.Publications((x) => x.WithTopic<OrderPlaced>());
            config.Subscriptions((x) => x.ForTopic<OrderPlaced>());
        });
        services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
        services.AddJustSayingAsyncApi();

        using var document = await GenerateAsync(services.BuildServiceProvider());
        var root = document.RootElement;

        var channels = root.GetProperty("channels");
        var topicChannel = channels.GetProperty("orderplaced");
        var queueChannel = channels.GetProperty("orderplaced-queue");

        await Assert.That(topicChannel.GetProperty("address").GetString()).IsEqualTo("orderplaced");
        await Assert.That(queueChannel.GetProperty("address").GetString()).IsEqualTo("orderplaced");

        // The topic channel is served by SNS and the queue channel by SQS.
        await Assert.That(topicChannel.GetProperty("servers")[0].GetProperty("$ref").GetString()).IsEqualTo("#/servers/sns");
        await Assert.That(queueChannel.GetProperty("servers")[0].GetProperty("$ref").GetString()).IsEqualTo("#/servers/sqs");

        var operations = root.GetProperty("operations");
        await Assert.That(operations.GetProperty("send-orderplaced").GetProperty("channel").GetProperty("$ref").GetString()).IsEqualTo("#/channels/orderplaced");
        await Assert.That(operations.GetProperty("receive-orderplaced-queue").GetProperty("channel").GetProperty("$ref").GetString()).IsEqualTo("#/channels/orderplaced-queue");
    }

    [Test]
    public async Task ARecursiveMessageTypeIsInlinedRatherThanDropped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) => x.WithTopic<Category>());
        });
        services.AddJustSayingAsyncApi();

        using var document = await GenerateAsync(services.BuildServiceProvider());
        var root = document.RootElement;

        var payload = root.GetProperty("channels").GetProperty("category").GetProperty("messages").EnumerateObject().First().Value.GetProperty("payload");

        // The self-referential Parent is inlined one level deep instead of being dropped as an
        // empty schema: the nested Parent still carries its own properties.
        var parent = payload.GetProperty("properties").GetProperty("Parent");
        await Assert.That(parent.TryGetProperty("properties", out var parentProperties)).IsTrue();
        await Assert.That(parentProperties.GetProperty("Parent").TryGetProperty("properties", out var nestedProperties)).IsTrue();
        await Assert.That(nestedProperties.TryGetProperty("Name", out _)).IsTrue();
    }
}
