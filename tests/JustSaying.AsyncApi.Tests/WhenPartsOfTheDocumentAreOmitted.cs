using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Metadata;
using Microsoft.Extensions.Logging;

namespace JustSaying.AsyncApi.Tests;

public class WhenPartsOfTheDocumentAreOmitted
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderPlacedV2
    {
        public Guid OrderId { get; set; }
    }

    private sealed class CapturingLogger : ILogger<AsyncApiDocumentGenerator>
    {
        public List<string> Warnings { get; } = [];

        public List<int> EventIds { get; } = [];

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
                EventIds.Add(eventId.Id);
            }
        }
    }

    [Test]
    public async Task AnEmptyRegistryLogsAWarning()
    {
        var logger = new CapturingLogger();
        var generator = new AsyncApiDocumentGenerator(new MessagingMetadataRegistry(), new AsyncApiOptions(), logger: logger);

        generator.Generate();

        await Assert.That(logger.Warnings).Contains((warning) => warning.Contains("no publications or subscriptions were captured"));
        await Assert.That(logger.EventIds).Contains(101);
    }

    [Test]
    public async Task ADynamicPublicationLogsAWarning()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            destinationName: null,
            isDynamic: true,
            [new MessageTypeMetadata(typeof(OrderPlaced), nameof(OrderPlaced))]));

        var logger = new CapturingLogger();
        var generator = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions(), logger: logger);

        var document = generator.Generate();

        await Assert.That(document.Channels).IsEmpty();
        await Assert.That(logger.Warnings).Contains((warning) => warning.Contains("dynamic destination") && warning.Contains(nameof(OrderPlaced)));
        await Assert.That(logger.EventIds).Contains(102);
    }

    [Test]
    public async Task TwoMessagesWithOneNameOnADestinationLogAWarningAndTheFirstIsDocumented()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "orders",
            isDynamic: false,
            [new MessageTypeMetadata(typeof(OrderPlaced), nameof(OrderPlaced))]));
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "orders",
            isDynamic: false,
            [new MessageTypeMetadata(typeof(OrderPlacedV2), nameof(OrderPlaced))]));

        var logger = new CapturingLogger();
        var generator = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions(), logger: logger);

        var document = generator.Generate();

        await Assert.That(logger.EventIds).Contains(107);
        await Assert.That(logger.Warnings).Contains((warning) => warning.Contains(nameof(OrderPlacedV2)) && warning.Contains("'OrderPlaced'") && warning.Contains("orders"));

        // The channel keeps the first registration, as the send operation does.
        var message = document.Channels["orders"].Messages[nameof(OrderPlaced)];
        await Assert.That(message.Title).IsEqualTo(nameof(OrderPlaced));
    }

    public static class Billing
    {
        public sealed class Thing
        {
        }
    }

    public static class Shipping
    {
        public sealed class Thing
        {
        }
    }

    public sealed class Envelope<T>
    {
        public T Body { get; set; }
    }

    private static List<string> DuplicateNameWarnings(params MessageTypeMetadata[] messages)
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        foreach (var message in messages)
        {
            registry.AddPublication(new PublicationMetadata(MessagingDestinationKind.SnsTopic, "things", isDynamic: false, [message]));
        }

        var logger = new CapturingLogger();
        new AsyncApiDocumentGenerator(registry, new AsyncApiOptions(), logger: logger).Generate();

        return logger.Warnings;
    }

    [Test]
    public async Task TwoMessagesWithOneNameAreNamedInFullInTheWarning()
    {
        var warnings = DuplicateNameWarnings(
            new MessageTypeMetadata(typeof(Billing.Thing), "Thing"),
            new MessageTypeMetadata(typeof(Shipping.Thing), "Thing"));

        await Assert.That(warnings).Contains(
            "JustSaying.AsyncApi.Tests.WhenPartsOfTheDocumentAreOmitted.Shipping.Thing and JustSaying.AsyncApi.Tests.WhenPartsOfTheDocumentAreOmitted.Billing.Thing " +
            "are both identified as 'Thing' on things, so consumers cannot tell them apart; " +
            "only JustSaying.AsyncApi.Tests.WhenPartsOfTheDocumentAreOmitted.Billing.Thing is documented. Give each message on a destination a distinct name.");
    }

    [Test]
    public async Task TwoGenericMessagesWithOneNameAreNamedInFullInTheWarning()
    {
        var warnings = DuplicateNameWarnings(
            new MessageTypeMetadata(typeof(Envelope<Billing.Thing>), "Envelope"),
            new MessageTypeMetadata(typeof(Envelope<Shipping.Thing>), "Envelope"));

        await Assert.That(warnings).Contains((warning) => warning.StartsWith(
            "JustSaying.AsyncApi.Tests.WhenPartsOfTheDocumentAreOmitted.Envelope<JustSaying.AsyncApi.Tests.WhenPartsOfTheDocumentAreOmitted.Shipping.Thing> and " +
            "JustSaying.AsyncApi.Tests.WhenPartsOfTheDocumentAreOmitted.Envelope<JustSaying.AsyncApi.Tests.WhenPartsOfTheDocumentAreOmitted.Billing.Thing> are both identified as 'Envelope'",
            StringComparison.Ordinal));
    }

    [Test]
    public async Task ANonSystemTextJsonSerializerLogsAWarning()
    {
#pragma warning disable IL2026, IL3050
        var serializer = new NewtonsoftMessageBodySerializer<OrderPlaced>();
#pragma warning restore IL2026, IL3050
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "order-placed",
            isDynamic: false,
            [new MessageTypeMetadata(typeof(OrderPlaced), nameof(OrderPlaced), serializer)]));

        var logger = new CapturingLogger();
        var generator = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions(), logger);

        var document = generator.Generate();

        await Assert.That(logger.Warnings).Contains((warning) => warning.Contains("NewtonsoftMessageBodySerializer<OrderPlaced>") && warning.Contains("documented without payload schemas"));
        await Assert.That(logger.EventIds).Contains(105);
        await Assert.That(document.Channels["order-placed"].Messages[nameof(OrderPlaced)].ContentType).IsEqualTo("application/json");
    }

    private sealed class OpaqueSerializer : IMessageBodySerializer<OrderPlaced>
    {
        public string Serialize(OrderPlaced message) => message.OrderId;

        public OrderPlaced Deserialize(string message) => new() { OrderId = message };
    }

    [Test]
    public async Task ASerializerThatDoesNotDescribeItsFormatLogsAWarning()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "order-placed",
            isDynamic: false,
            [new MessageTypeMetadata(typeof(OrderPlaced), nameof(OrderPlaced), new OpaqueSerializer())]));

        var logger = new CapturingLogger();
        var generator = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions(), logger);

        var document = generator.Generate();

        await Assert.That(logger.Warnings).Contains((warning) => warning.Contains(nameof(IMessageBodyFormat)));
        await Assert.That(logger.EventIds).Contains(106);
        await Assert.That(document.Channels["order-placed"].Messages[nameof(OrderPlaced)].Payload).IsNull();
    }

    private sealed class XmlSerializer : IMessageBodySerializer<OrderPlaced>, IMessageBodyFormat
    {
        public string ContentType => "application/xml";

        public string Serialize(OrderPlaced message) => $"<OrderPlaced>{message.OrderId}</OrderPlaced>";

        public OrderPlaced Deserialize(string message) => new();
    }

    [Test]
    public async Task ASerializerThatDescribesItsFormatIsDocumentedWithItsContentType()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "order-placed",
            isDynamic: false,
            [new MessageTypeMetadata(typeof(OrderPlaced), nameof(OrderPlaced), new XmlSerializer())]));

        var document = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions()).Generate();

        await Assert.That(document.Channels["order-placed"].Messages[nameof(OrderPlaced)].ContentType).IsEqualTo("application/xml");
    }
}
