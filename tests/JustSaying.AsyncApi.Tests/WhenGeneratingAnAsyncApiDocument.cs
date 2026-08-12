using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenGeneratingAnAsyncApiDocument
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
        public decimal Total { get; set; }
    }

    public sealed class OrderReady
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
    {
        public Task<bool> Handle(OrderPlaced message) => Task.FromResult(true);
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) => x.WithTopic<OrderReady>());
            config.Subscriptions((x) => x.ForTopic<OrderPlaced>());
        });
        services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
        services.AddJustSayingAsyncApi((options) =>
        {
            options.Title = "Orders";
            options.Version = "2.1.0";
        });

        return services.BuildServiceProvider();
    }

    private static async Task<JsonDocument> GenerateAsync(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return JsonDocument.Parse(writer.ToString());
    }

    [Test]
    public async Task TheDocumentDescribesTheBus()
    {
        using var document = await GenerateAsync(BuildServiceProvider());
        var root = document.RootElement;

        await Assert.That(root.GetProperty("asyncapi").GetString()).StartsWith("3.");
        await Assert.That(root.GetProperty("info").GetProperty("title").GetString()).IsEqualTo("Orders");
        await Assert.That(root.GetProperty("info").GetProperty("version").GetString()).IsEqualTo("2.1.0");

        var servers = root.GetProperty("servers");
        await Assert.That(servers.GetProperty("sns").GetProperty("host").GetString()).IsEqualTo("sns.eu-west-1.amazonaws.com");
        await Assert.That(servers.GetProperty("sqs").GetProperty("protocol").GetString()).IsEqualTo("sqs");
    }

    [Test]
    public async Task TheTopicPublicationBecomesASendOperation()
    {
        using var document = await GenerateAsync(BuildServiceProvider());
        var root = document.RootElement;

        var channel = root.GetProperty("channels").GetProperty("orderready");
        await Assert.That(channel.GetProperty("address").GetString()).IsEqualTo("orderready");

        var operation = root.GetProperty("operations").GetProperty("send-orderready");
        await Assert.That(operation.GetProperty("action").GetString()).IsEqualTo("send");
        await Assert.That(operation.GetProperty("channel").GetProperty("$ref").GetString()).IsEqualTo("#/channels/orderready");
    }

    [Test]
    public async Task TheTopicSubscriptionBecomesAReceiveOperationOnTheQueueChannel()
    {
        using var document = await GenerateAsync(BuildServiceProvider());
        var root = document.RootElement;

        var channel = root.GetProperty("channels").GetProperty("orderplaced");
        await Assert.That(channel.GetProperty("address").GetString()).IsEqualTo("orderplaced");
        await Assert.That(channel.GetProperty("description").GetString()).Contains("subscribed to the orderplaced SNS topic");

        var operation = root.GetProperty("operations").GetProperty("receive-orderplaced");
        await Assert.That(operation.GetProperty("action").GetString()).IsEqualTo("receive");
    }

    [Test]
    public async Task TheMessagePayloadHasASchema()
    {
        using var document = await GenerateAsync(BuildServiceProvider());
        var root = document.RootElement;

        var message = root.GetProperty("channels").GetProperty("orderplaced").GetProperty("messages").EnumerateObject().First().Value;
        var payload = message.GetProperty("payload");

        await Assert.That(payload.GetProperty("type").GetString()).IsEqualTo("object");
        var properties = payload.GetProperty("properties");
        await Assert.That(properties.TryGetProperty("OrderId", out _)).IsTrue();
        await Assert.That(properties.TryGetProperty("Total", out _)).IsTrue();
    }

    [Test]
    public async Task TheBusIsNotStartedByDocumentGeneration()
    {
        var serviceProvider = BuildServiceProvider();
        using var document = await GenerateAsync(serviceProvider);

        // Interrogation exposes the subscription groups only once the bus has started.
        var bus = serviceProvider.GetRequiredService<IMessagingBus>();
        using var data = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(bus.Interrogate()));
        var root = data.RootElement.TryGetProperty("Data", out var unwrapped) ? unwrapped : data.RootElement;
        await Assert.That(root.GetProperty("SubscriptionGroups").ValueKind).IsEqualTo(JsonValueKind.Null);
    }
}
