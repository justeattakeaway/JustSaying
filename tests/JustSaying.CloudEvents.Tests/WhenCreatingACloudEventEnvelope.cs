namespace JustSaying.CloudEvents.Tests;

public class WhenCreatingACloudEventEnvelope
{
    private sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task ExtensionsDefaultToAnEmptyReadOnlyDictionary()
    {
        var cloudEvent = new CloudEvent<OrderPlaced>(new OrderPlaced { OrderId = "1" });

        await Assert.That(cloudEvent.Extensions).IsNotNull();
        await Assert.That(cloudEvent.Extensions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TheDefaultExtensionsCannotBeMutatedThroughACast()
    {
        var cloudEvent = new CloudEvent<OrderPlaced>(new OrderPlaced { OrderId = "1" });

        // The empty instance is shared across every envelope created without extensions, so a mutable
        // Dictionary behind the interface would leak data between unrelated messages.
        await Assert.That(cloudEvent.Extensions as Dictionary<string, string>).IsNull();

        var mutable = (IDictionary<string, string>)cloudEvent.Extensions;
        await Assert.That(() => mutable.Add("tenantid", "uk")).Throws<NotSupportedException>();

        var another = new CloudEvent<OrderPlaced>(new OrderPlaced { OrderId = "2" });
        await Assert.That(another.Extensions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SuppliedExtensionsAreExposed()
    {
        var cloudEvent = new CloudEvent<OrderPlaced>(
            new OrderPlaced { OrderId = "1" },
            extensions: new Dictionary<string, string>(StringComparer.Ordinal) { ["tenantid"] = "uk" });

        await Assert.That(cloudEvent.Extensions["tenantid"]).IsEqualTo("uk");
    }
}
