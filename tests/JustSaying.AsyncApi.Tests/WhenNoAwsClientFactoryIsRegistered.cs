using System.Text.Json;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenNoAwsClientFactoryIsRegistered
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
    {
        public Task<bool> Handle(OrderPlaced message) => Task.FromResult(true);
    }

    [Test]
    public async Task TheDocumentIsGeneratedWithTheDefaultClientFactory()
    {
        // Generation makes no AWS calls, so falling back to DefaultAwsClientFactory must work
        // even where no AWS credentials can be resolved (for example on a CI agent).
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) => x.WithTopic<OrderPlaced>());
            config.Subscriptions((x) => x.ForTopic<OrderPlaced>());
        });
        services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
        services.AddJustSayingAsyncApi();

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var root = document.RootElement;
        await Assert.That(root.GetProperty("channels").TryGetProperty("orderplaced", out _)).IsTrue();
        await Assert.That(root.GetProperty("operations").TryGetProperty("send-orderplaced", out _)).IsTrue();
    }
}
