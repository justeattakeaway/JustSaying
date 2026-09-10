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

    private sealed class CapturingLogger : ILogger<AsyncApiDocumentGenerator>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
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
    }

    [Test]
    public async Task ANonSystemTextJsonSerializerLogsAWarning()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "order-placed",
            isDynamic: false,
            [new MessageTypeMetadata(typeof(OrderPlaced), nameof(OrderPlaced))]));

        var logger = new CapturingLogger();
#pragma warning disable IL2026, IL3050
        var serializationFactory = new NewtonsoftSerializationFactory();
#pragma warning restore IL2026, IL3050
        var generator = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions(), serializationFactory, logger: logger);

        generator.Generate();

        await Assert.That(logger.Warnings).Contains((warning) => warning.Contains("documented without payload schemas"));
    }
}
