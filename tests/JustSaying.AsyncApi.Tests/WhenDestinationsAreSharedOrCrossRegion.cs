using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenDestinationsAreSharedOrCrossRegion
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderReady
    {
        public string OrderId { get; set; }
    }

    private static async Task<JsonDocument> GenerateAsync(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return JsonDocument.Parse(writer.ToString());
    }

    [Test]
    public async Task PublicationsToTheSameTopicMergeIntoOneSendOperation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) =>
            {
                x.WithTopic<OrderPlaced>((c) => c.WithTopicName("orders"));
                x.WithTopic<OrderReady>((c) => c.WithTopicName("orders"));
            });
        });
        services.AddJustSayingAsyncApi();

        using var document = await GenerateAsync(services.BuildServiceProvider());
        var root = document.RootElement;

        var channelMessages = root.GetProperty("channels").GetProperty("orders").GetProperty("messages");
        await Assert.That(channelMessages.TryGetProperty("OrderPlaced", out _)).IsTrue();
        await Assert.That(channelMessages.TryGetProperty("OrderReady", out _)).IsTrue();

        // The second publication must not overwrite the first's message references.
        var operation = root.GetProperty("operations").GetProperty("send-orders");
        var references = operation.GetProperty("messages").EnumerateArray().Select((m) => m.GetProperty("$ref").GetString()).ToList();
        await Assert.That(references).Contains("#/channels/orders/messages/OrderPlaced");
        await Assert.That(references).Contains("#/channels/orders/messages/OrderReady");
    }

    [Test]
    public async Task ACrossRegionTopicGetsItsOwnServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) =>
            {
                x.WithTopic<OrderPlaced>();
                x.WithTopicArn<OrderReady>("arn:aws:sns:us-east-1:123456789012:external-orders");
            });
        });
        services.AddJustSayingAsyncApi();

        using var document = await GenerateAsync(services.BuildServiceProvider());
        var root = document.RootElement;

        // The bus's own region keeps the plain server key; the cross-region topic must not
        // be documented against it.
        var servers = root.GetProperty("servers");
        await Assert.That(servers.GetProperty("sns").GetProperty("host").GetString()).IsEqualTo("sns.eu-west-1.amazonaws.com");
        await Assert.That(servers.GetProperty("sns-us-east-1").GetProperty("host").GetString()).IsEqualTo("sns.us-east-1.amazonaws.com");

        var channels = root.GetProperty("channels");
        await Assert.That(channels.GetProperty("orderplaced").GetProperty("servers")[0].GetProperty("$ref").GetString()).IsEqualTo("#/servers/sns");
        await Assert.That(channels.GetProperty("external-orders").GetProperty("servers")[0].GetProperty("$ref").GetString()).IsEqualTo("#/servers/sns-us-east-1");
    }
}
