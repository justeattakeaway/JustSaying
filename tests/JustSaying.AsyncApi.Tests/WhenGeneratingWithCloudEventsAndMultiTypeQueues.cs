using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenGeneratingWithCloudEventsAndMultiTypeQueues
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderCancelled
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
    {
        public Task<bool> Handle(OrderPlaced message) => Task.FromResult(true);
    }

    public sealed class OrderCancelledHandler : IHandlerAsync<OrderCancelled>
    {
        public Task<bool> Handle(OrderCancelled message) => Task.FromResult(true);
    }

    private static async Task<JsonDocument> GenerateAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSayingCloudEvents((options) =>
        {
            options.Source = new Uri("https://orders.example.com/");
            options.WithCloudEventType<OrderPlaced>("com.example.orders.placed");
            options.WithCloudEventType<OrderCancelled>("com.example.orders.cancelled");
        });
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) => x.WithTopic<OrderPlaced>());
            config.Subscriptions((x) => x.ForQueue("orders", (q) =>
                q.Handling<OrderPlaced>("com.example.orders.placed")
                    .Handling<OrderCancelled>("com.example.orders.cancelled")));
        });
        services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
        services.AddJustSayingHandler<OrderCancelled, OrderCancelledHandler>();
        services.AddJustSayingAsyncApi();

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return JsonDocument.Parse(writer.ToString());
    }

    [Test]
    public async Task MessagesAreNamedByTheirCloudEventType()
    {
        using var document = await GenerateAsync();
        var root = document.RootElement;

        var messages = root.GetProperty("channels").GetProperty("orderplaced").GetProperty("messages");
        await Assert.That(messages.TryGetProperty("com.example.orders.placed", out var message)).IsTrue();
        await Assert.That(message.GetProperty("name").GetString()).IsEqualTo("com.example.orders.placed");
        await Assert.That(message.GetProperty("contentType").GetString()).IsEqualTo("application/cloudevents+json");
    }

    [Test]
    public async Task ThePayloadDocumentsTheCloudEventsEnvelope()
    {
        using var document = await GenerateAsync();
        var root = document.RootElement;

        var message = root.GetProperty("channels").GetProperty("orderplaced").GetProperty("messages").GetProperty("com.example.orders.placed");
        var payload = message.GetProperty("payload");

        // The wire format is the CloudEvents structured-mode envelope, not the bare data schema.
        var properties = payload.GetProperty("properties");
        await Assert.That(properties.GetProperty("specversion").GetProperty("const").GetString()).IsEqualTo("1.0");
        await Assert.That(properties.GetProperty("type").GetProperty("const").GetString()).IsEqualTo("com.example.orders.placed");
        await Assert.That(properties.GetProperty("source").GetProperty("format").GetString()).IsEqualTo("uri-reference");

        var required = payload.GetProperty("required").EnumerateArray().Select((r) => r.GetString()).ToList();
        await Assert.That(required).Contains("data");

        var dataProperties = properties.GetProperty("data").GetProperty("properties");
        await Assert.That(dataProperties.TryGetProperty("OrderId", out _)).IsTrue();
    }

    [Test]
    public async Task AMultiTypeQueueBecomesOneChannelWithAMessagesMap()
    {
        using var document = await GenerateAsync();
        var root = document.RootElement;

        var channel = root.GetProperty("channels").GetProperty("orders");
        var messages = channel.GetProperty("messages");
        await Assert.That(messages.TryGetProperty("com.example.orders.placed", out _)).IsTrue();
        await Assert.That(messages.TryGetProperty("com.example.orders.cancelled", out _)).IsTrue();

        var operation = root.GetProperty("operations").GetProperty("receive-orders");
        await Assert.That(operation.GetProperty("messages").GetArrayLength()).IsEqualTo(2);
    }
}
