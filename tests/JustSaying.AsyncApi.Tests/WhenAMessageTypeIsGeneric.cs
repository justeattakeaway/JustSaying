using JustSaying.Messaging.Metadata;

namespace JustSaying.AsyncApi.Tests;

public class WhenAMessageTypeIsGeneric
{
    public sealed class OrderReady
    {
        public string OrderId { get; set; }
    }

    public sealed class Envelope<T>
    {
        public T Body { get; set; }
    }

    [Test]
    public async Task TheTitleAndSummaryExpandTheGenericArguments()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "envelopeorderready",
            isDynamic: false,
            [new MessageTypeMetadata(typeof(Envelope<OrderReady>), typeof(Envelope<OrderReady>).Name)]));

        var generator = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions());
        var document = generator.Generate();

        var message = document.Channels["envelopeorderready"].Messages.Single().Value;

        // The title is for humans, so the closed generic is expanded; the name stays faithful
        // to the wire discriminator, which is the registered logical name (Type.Name here).
        await Assert.That(message.Title).IsEqualTo("Envelope<OrderReady>");
        await Assert.That(message.Name).IsEqualTo("Envelope`1");
        await Assert.That(document.Operations["send-envelopeorderready"].Summary).Contains("Envelope<OrderReady>");
    }
}
