using System.Text.Json;
using System.Text.Json.Nodes;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenCustomisingTheDocument
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderReady
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
    {
        public Task<bool> Handle(OrderPlaced message) => Task.FromResult(true);
    }

    private static async Task<string> GenerateAsync(Action<AsyncApiOptions> configure)
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
        services.AddJustSayingAsyncApi(configure);

        var provider = services.BuildServiceProvider().GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return writer.ToString();
    }

    [Test]
    public async Task TheInfoIsWrittenFromTheOptions()
    {
        using var document = JsonDocument.Parse(await GenerateAsync((options) =>
        {
            options.Description = "Order events.";
            options.TermsOfService = new Uri("https://example.com/terms");
            options.Contact = new AsyncApiContactOptions() { Name = "Orders team", Url = new Uri("https://example.com/orders"), Email = "orders@example.com" };
            options.License = new AsyncApiLicenseOptions() { Name = "Apache-2.0", Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0") };
            options.Tags.Add(new AsyncApiTagOptions() { Name = "orders", Description = "Order lifecycle." });
            options.ExternalDocs = new AsyncApiExternalDocsOptions() { Url = new Uri("https://example.com/docs"), Description = "Guide" };
        }));

        var info = document.RootElement.GetProperty("info");
        await Assert.That(info.GetProperty("description").GetString()).IsEqualTo("Order events.");
        await Assert.That(info.GetProperty("termsOfService").GetString()).IsEqualTo("https://example.com/terms");
        await Assert.That(info.GetProperty("contact").GetProperty("name").GetString()).IsEqualTo("Orders team");
        await Assert.That(info.GetProperty("contact").GetProperty("url").GetString()).IsEqualTo("https://example.com/orders");
        await Assert.That(info.GetProperty("contact").GetProperty("email").GetString()).IsEqualTo("orders@example.com");
        await Assert.That(info.GetProperty("license").GetProperty("name").GetString()).IsEqualTo("Apache-2.0");
        await Assert.That(info.GetProperty("tags")[0].GetProperty("name").GetString()).IsEqualTo("orders");
        await Assert.That(info.GetProperty("tags")[0].GetProperty("description").GetString()).IsEqualTo("Order lifecycle.");
        await Assert.That(info.GetProperty("externalDocs").GetProperty("url").GetString()).IsEqualTo("https://example.com/docs");
    }

    [Test]
    public async Task ConfiguredServersReplaceTheGeneratedOnesAndChannelsAreBoundByProtocol()
    {
        using var document = JsonDocument.Parse(await GenerateAsync((options) =>
        {
            options.Servers["production-sns"] = new AsyncApiServerOptions() { Host = "sns.eu-west-1.amazonaws.com", Protocol = "sns", Description = "Production" };
            options.Servers["production-sqs"] = new AsyncApiServerOptions() { Host = "sqs.eu-west-1.amazonaws.com", Protocol = "sqs", Description = "Production" };
            options.Servers["local"] = new AsyncApiServerOptions() { Host = "localhost:4566", Protocol = "sqs", Description = "LocalStack" };
        }));

        var root = document.RootElement;
        await Assert.That(root.GetProperty("servers").EnumerateObject().Select((s) => s.Name).ToList())
            .IsEquivalentTo(["production-sns", "production-sqs", "local"]);

        static List<string> ServersOf(JsonElement channel)
            => [.. channel.GetProperty("servers").EnumerateArray().Select((s) => s.GetProperty("$ref").GetString())];

        // The orderready topic channel is served by the SNS server, the orderplaced queue channel by both SQS servers.
        var channels = root.GetProperty("channels");
        await Assert.That(ServersOf(channels.GetProperty("orderready"))).IsEquivalentTo(["#/servers/production-sns"]);
        await Assert.That(ServersOf(channels.GetProperty("orderplaced"))).IsEquivalentTo(["#/servers/production-sqs", "#/servers/local"]);
    }

    [Test]
    public async Task AnInvalidServerNameFailsGeneration()
    {
        await Assert.That(async () => await GenerateAsync((options) =>
                options.Servers["my server"] = new AsyncApiServerOptions() { Host = "localhost", Protocol = "sqs" }))
            .Throws<InvalidOperationException>()
            .WithMessageContaining("my server");
    }

    [Test]
    public async Task PostProcessCanChangeTheDocumentAsJson()
    {
        var json = await GenerateAsync((options) => options.PostProcess = (document) =>
        {
            document["info"]!["x-owner"] = "orders";
            document["channels"]!.AsObject().Remove("orderready");
        });

        using var document = JsonDocument.Parse(json);
        await Assert.That(document.RootElement.GetProperty("info").GetProperty("x-owner").GetString()).IsEqualTo("orders");
        await Assert.That(document.RootElement.GetProperty("channels").TryGetProperty("orderready", out _)).IsFalse();

        // The document keeps the layout it is generated in.
        await Assert.That(json).DoesNotContain("\r\n");
        await Assert.That(json).StartsWith("{\n  \"asyncapi\": \"3.1.0\"");
    }

    [Test]
    public async Task WithoutCustomisationTheDocumentIsUnchangedByPostProcessing()
    {
        var plain = await GenerateAsync((_) => { });
        var roundTripped = await GenerateAsync((options) => options.PostProcess = (_) => { });

        await Assert.That(JsonNode.DeepEquals(JsonNode.Parse(roundTripped), JsonNode.Parse(plain))).IsTrue();
    }
}
