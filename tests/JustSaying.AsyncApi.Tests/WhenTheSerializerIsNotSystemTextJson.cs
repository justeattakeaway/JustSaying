using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenTheSerializerIsNotSystemTextJson
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    private static IServiceProvider BuildServiceProvider(Action<AsyncApiOptions> configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
#pragma warning disable IL2026, IL3050
        services.AddSingleton<IMessageBodySerializationFactory>(new NewtonsoftSerializationFactory());
#pragma warning restore IL2026, IL3050
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) => x.WithTopic<OrderPlaced>());
        });
        services.AddJustSayingAsyncApi(configure);

        return services.BuildServiceProvider();
    }

    private static async Task<JsonDocument> GenerateAsync(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return JsonDocument.Parse(writer.ToString());
    }

    [Test]
    public async Task NoPayloadSchemaIsInferredForANewtonsoftSerializer()
    {
        using var document = await GenerateAsync(BuildServiceProvider());
        var root = document.RootElement;

        // A System.Text.Json-shaped schema could misdocument Newtonsoft's wire format (for
        // example enums-as-strings), so the message is documented without a payload schema.
        var message = root.GetProperty("channels").GetProperty("orderplaced").GetProperty("messages").EnumerateObject().First().Value;
        await Assert.That(message.TryGetProperty("payload", out _)).IsFalse();
    }

    [Test]
    public async Task ExplicitSerializerOptionsStillProduceAPayloadSchema()
    {
        using var document = await GenerateAsync(BuildServiceProvider((options) =>
            options.SerializerOptions = JsonSerializerOptions.Default));
        var root = document.RootElement;

        var message = root.GetProperty("channels").GetProperty("orderplaced").GetProperty("messages").EnumerateObject().First().Value;
        await Assert.That(message.GetProperty("payload").GetProperty("properties").TryGetProperty("OrderId", out _)).IsTrue();
    }
}
