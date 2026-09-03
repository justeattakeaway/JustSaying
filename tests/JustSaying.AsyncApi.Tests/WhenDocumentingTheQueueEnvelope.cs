using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
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

    public sealed class ParcelShipped
    {
        public string ParcelId { get; set; }
    }

    public sealed class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
    {
        public Task<bool> Handle(OrderPlaced message) => Task.FromResult(true);
    }

    private static async Task<JsonDocument> GenerateAsync(bool rawMessages, bool subscribe = false, bool mixedQueue = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSayingCloudEvents((options) => options.Source = new Uri("https://orders.example.com/"));
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

                if (mixedQueue)
                {
                    // A self-describing CloudEvent is never wrapped, even on the same queue.
                    x.WithCloudEventQueue<ParcelShipped>(QueueDestination.Named("orderplaced"), "com.example.parcels.shipped");
                }
            });

            if (subscribe)
            {
                config.Subscriptions((x) => x.ForQueue<OrderPlaced>((queue) =>
                {
                    if (rawMessages)
                    {
                        queue.WithRawMessageDelivery();
                    }
                }));
            }
        });
        services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
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

    [Test]
    public async Task AMixedQueueDescribesWhichMessagesAreWrapped()
    {
        using var document = await GenerateAsync(rawMessages: false, mixedQueue: true);

        var operation = document.RootElement.GetProperty("operations").GetProperty("send-orderplaced");
        var description = operation.GetProperty("description").GetString();
        await Assert.That(description).Contains("wraps OrderPlaced in JustSaying's queue envelope");
        await Assert.That(description).Contains("ParcelShipped is sent verbatim");
    }

    [Test]
    public async Task ADefaultQueueSubscriptionDescribesBothBodyShapes()
    {
        using var document = await GenerateAsync(rawMessages: false, subscribe: true);

        // The consumer cannot know whether the producer wraps, so it accepts either shape.
        var operation = document.RootElement.GetProperty("operations").GetProperty("receive-orderplaced");
        var description = operation.GetProperty("description").GetString();
        await Assert.That(description).Contains("either as the documented message payload or wrapped in JustSaying's queue envelope");
    }

    [Test]
    public async Task ARawQueueSubscriptionDescribesRawDelivery()
    {
        using var document = await GenerateAsync(rawMessages: true, subscribe: true);

        var operation = document.RootElement.GetProperty("operations").GetProperty("receive-orderplaced");
        var description = operation.GetProperty("description").GetString();
        await Assert.That(description).Contains("raw message delivery");
        await Assert.That(description).DoesNotContain("queue envelope");
    }
}
