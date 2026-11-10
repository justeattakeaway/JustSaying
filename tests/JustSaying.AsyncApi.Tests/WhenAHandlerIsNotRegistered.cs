using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenAHandlerIsNotRegistered
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task TheErrorExplainsThatDocumentGenerationBuildsTheBus()
    {
        // Building the bus resolves every subscription's handler eagerly, so a missing handler
        // fails document generation. The surprise of a docs call requiring handlers is
        // softened by an error that says what happened and what to register.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Subscriptions((x) => x.ForTopic<OrderPlaced>());
        });
        services.AddJustSayingAsyncApi();

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GenerateAsync(provider.GetDocumentNames()[0], writer));

        await Assert.That(exception.Message).Contains("builds the JustSaying messaging bus");
        await Assert.That(exception.Message).Contains("AddJustSayingHandler");
        await Assert.That(exception.InnerException).IsNotNull();
        await Assert.That(exception.InnerException!.Message).Contains(nameof(OrderPlaced));
    }
}
