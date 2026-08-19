using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenDocumentingTheQueueEnvelope
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderDispatched
    {
        public string OrderId { get; set; }
    }

    private static async Task<JsonDocument> GenerateAsync(bool rawMessages)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Publications((x) =>
            {
                x.WithQueue<OrderPlaced>((queue) =>
                {
                    if (rawMessages)
                    {
                        queue.WithRawMessages();
                    }
                });
                x.WithTopic<OrderDispatched>();
            });
        });
        services.AddJustSayingAsyncApi();

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return JsonDocument.Parse(writer.ToString());
    }

    [Test]
    public async Task ADefaultQueuePublicationDescribesTheQueueEnvelope()
    {
        using var document = await GenerateAsync(rawMessages: false);

        var operation = document.RootElement.GetProperty("operations").GetProperty("send-orderplaced");
        var description = operation.GetProperty("description").GetString();
        await Assert.That(description).Contains("JustSaying's queue envelope");
        await Assert.That(description).Contains("\"Message\"");
    }

    [Test]
    public async Task ARawQueuePublicationHasNoEnvelopeDescription()
    {
        using var document = await GenerateAsync(rawMessages: true);

        // The SQS body is the payload itself, which is what the message schema already documents.
        var operation = document.RootElement.GetProperty("operations").GetProperty("send-orderplaced");
        await Assert.That(operation.TryGetProperty("description", out _)).IsFalse();
    }

    [Test]
    public async Task ATopicPublicationHasNoEnvelopeDescription()
    {
        using var document = await GenerateAsync(rawMessages: false);

        // Only SQS publications use the queue envelope; SNS carries the payload verbatim.
        var operation = document.RootElement.GetProperty("operations").GetProperty("send-orderdispatched");
        await Assert.That(operation.TryGetProperty("description", out _)).IsFalse();
    }
}
