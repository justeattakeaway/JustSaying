using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

/// <summary>
/// CloudEvents is opted into per registration, so the document must describe each message from the
/// serializer its registration actually uses: a <c>WithCloudEventTopic</c> publication and the
/// <c>HandlingCloudEvent</c>/<c>HandlingCloudEventData</c> subscriptions are CloudEvents, while a plain
/// <c>WithTopic</c> in the same application stays plain JSON.
/// </summary>
public class WhenGeneratingWithCloudEventsAndMultiTypeQueues
{
    private const string OrderPlacedType = "com.example.orders.placed";
    private const string OrderCancelledType = "com.example.orders.cancelled";

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderCancelled
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderReady
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderPlacedHandler : IHandlerAsync<CloudEvent<OrderPlaced>>
    {
        public Task<bool> Handle(CloudEvent<OrderPlaced> message) => Task.FromResult(true);
    }

    public sealed class OrderCancelledHandler : IHandlerAsync<OrderCancelled>
    {
        public Task<bool> Handle(OrderCancelled message) => Task.FromResult(true);
    }

    private static async Task<JsonDocument> GenerateAsync(bool useAsDefault = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSayingCloudEvents(
            (options) =>
            {
                options.Source = new Uri("https://orders.example.com/");
                options.MapType<OrderPlaced>(OrderPlacedType);
                options.MapType<OrderCancelled>(OrderCancelledType);
                options.MapType<OrderReady>("com.example.orders.ready");
            },
            useAsDefault: useAsDefault);
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) =>
            {
                x.WithCloudEventTopic<OrderPlaced>(OrderPlacedType);
                x.WithTopic<OrderReady>();
            });
            config.Subscriptions((x) => x.ForQueue("orders", (q) =>
                q.HandlingCloudEvent<OrderPlaced>(OrderPlacedType)
                    .HandlingCloudEventData<OrderCancelled>(OrderCancelledType)));
        });
        services.AddJustSayingHandler<CloudEvent<OrderPlaced>, OrderPlacedHandler>();
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
        await Assert.That(messages.TryGetProperty(OrderPlacedType, out var message)).IsTrue();
        await Assert.That(message.GetProperty("name").GetString()).IsEqualTo(OrderPlacedType);
        await Assert.That(message.GetProperty("title").GetString()).IsEqualTo(nameof(OrderPlaced));
        await Assert.That(message.GetProperty("contentType").GetString()).IsEqualTo("application/cloudevents+json");
    }

    [Test]
    public async Task BothPublishShapesOfACloudEventAreOneMessage()
    {
        using var document = await GenerateAsync();
        var root = document.RootElement;

        // WithCloudEventTopic<T> registers the bare T and the CloudEvent<T> envelope against one
        // topic; on the wire they are the same CloudEvent, so the document has one message.
        var messages = root.GetProperty("channels").GetProperty("orderplaced").GetProperty("messages");
        await Assert.That(messages.EnumerateObject().Count()).IsEqualTo(1);

        var operation = root.GetProperty("operations").GetProperty("send-orderplaced");
        await Assert.That(operation.GetProperty("messages").GetArrayLength()).IsEqualTo(1);
        await Assert.That(operation.GetProperty("summary").GetString()).IsEqualTo($"Publish {nameof(OrderPlaced)} to orderplaced.");
    }

    [Test]
    public async Task ThePayloadDocumentsTheCloudEventsEnvelope()
    {
        using var document = await GenerateAsync();
        var root = document.RootElement;

        var message = root.GetProperty("channels").GetProperty("orderplaced").GetProperty("messages").GetProperty(OrderPlacedType);
        var payload = message.GetProperty("payload");

        // The wire format is the CloudEvents structured-mode envelope, not the bare data schema.
        var properties = payload.GetProperty("properties");
        await Assert.That(properties.GetProperty("specversion").GetProperty("const").GetString()).IsEqualTo("1.0");
        await Assert.That(properties.GetProperty("type").GetProperty("const").GetString()).IsEqualTo(OrderPlacedType);
        await Assert.That(properties.GetProperty("source").GetProperty("format").GetString()).IsEqualTo("uri-reference");
        await Assert.That(properties.GetProperty("datacontenttype").GetProperty("const").GetString()).IsEqualTo("application/json");

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
        await Assert.That(messages.TryGetProperty(OrderPlacedType, out var placed)).IsTrue();
        await Assert.That(messages.TryGetProperty(OrderCancelledType, out var cancelled)).IsTrue();

        // Whether the handler receives the envelope or just its data, the wire format is a CloudEvent.
        await Assert.That(placed.GetProperty("title").GetString()).IsEqualTo(nameof(OrderPlaced));
        await Assert.That(placed.GetProperty("contentType").GetString()).IsEqualTo("application/cloudevents+json");
        await Assert.That(cancelled.GetProperty("contentType").GetString()).IsEqualTo("application/cloudevents+json");
        await Assert.That(cancelled.GetProperty("payload").GetProperty("properties").GetProperty("type").GetProperty("const").GetString()).IsEqualTo(OrderCancelledType);

        var operation = root.GetProperty("operations").GetProperty("receive-orders");
        await Assert.That(operation.GetProperty("messages").GetArrayLength()).IsEqualTo(2);
    }

    [Test]
    public async Task APlainPublicationInTheSameApplicationStaysPlainJson()
    {
        using var document = await GenerateAsync();
        var root = document.RootElement;

        // CloudEvents support was added without useAsDefault, so WithTopic<OrderReady> still
        // publishes plain JSON even though OrderReady has a CloudEvents type mapped.
        var message = root.GetProperty("channels").GetProperty("orderready").GetProperty("messages").GetProperty(nameof(OrderReady));
        await Assert.That(message.GetProperty("contentType").GetString()).IsEqualTo("application/json");
        await Assert.That(message.GetProperty("payload").GetProperty("properties").TryGetProperty("OrderId", out _)).IsTrue();
        await Assert.That(message.GetProperty("payload").GetProperty("properties").TryGetProperty("specversion", out _)).IsFalse();

        // Formats are mixed, so the document-level default is plain JSON.
        await Assert.That(root.GetProperty("defaultContentType").GetString()).IsEqualTo("application/json");
    }

    [Test]
    public async Task WhenCloudEventsIsTheDefaultEveryRegistrationIsACloudEvent()
    {
        using var document = await GenerateAsync(useAsDefault: true);
        var root = document.RootElement;

        var message = root.GetProperty("channels").GetProperty("orderready").GetProperty("messages").GetProperty("com.example.orders.ready");
        await Assert.That(message.GetProperty("contentType").GetString()).IsEqualTo("application/cloudevents+json");
        await Assert.That(message.GetProperty("payload").GetProperty("properties").GetProperty("type").GetProperty("const").GetString()).IsEqualTo("com.example.orders.ready");

        await Assert.That(root.GetProperty("defaultContentType").GetString()).IsEqualTo("application/cloudevents+json");
    }
}
