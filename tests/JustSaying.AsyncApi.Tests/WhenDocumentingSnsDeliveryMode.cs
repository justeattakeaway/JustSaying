using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.AsyncApi.Tests;

public class WhenDocumentingSnsDeliveryMode
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderDispatched
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
    {
        public Task<bool> Handle(OrderPlaced message) => Task.FromResult(true);
    }

    public sealed class OrderDispatchedHandler : IHandlerAsync<OrderDispatched>
    {
        public Task<bool> Handle(OrderDispatched message) => Task.FromResult(true);
    }

    private static async Task<JsonDocument> GenerateAsync(bool rawMessageDelivery)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAwsClientFactory>(new LocalAwsClientFactory(new InMemoryAwsBus()));
        services.AddJustSaying((config) =>
        {
            config.Messaging((x) => x.WithRegion("eu-west-1"));
            config.Subscriptions((x) =>
            {
                x.ForTopic<OrderPlaced>((topic) => topic.WithReadConfiguration((read) => read.RawMessageDelivery = rawMessageDelivery));
                x.ForQueue<OrderDispatched>();
            });
        });
        services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
        services.AddJustSayingHandler<OrderDispatched, OrderDispatchedHandler>();
        services.AddJustSayingAsyncApi();

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        using var writer = new StringWriter();
        await provider.GenerateAsync(provider.GetDocumentNames()[0], writer);
        return JsonDocument.Parse(writer.ToString());
    }

    [Test]
    public async Task AClassicTopicSubscriptionDescribesTheSnsNotificationEnvelope()
    {
        using var document = await GenerateAsync(rawMessageDelivery: false);
        var root = document.RootElement;

        var operation = root.GetProperty("operations").GetProperty("receive-orderplaced");
        var description = operation.GetProperty("description").GetString();
        await Assert.That(description).Contains("does not use raw message delivery");
        await Assert.That(description).Contains("Amazon SNS notification envelope");
    }

    [Test]
    public async Task ARawTopicSubscriptionDescribesRawMessageDelivery()
    {
        using var document = await GenerateAsync(rawMessageDelivery: true);
        var root = document.RootElement;

        var operation = root.GetProperty("operations").GetProperty("receive-orderplaced");
        var description = operation.GetProperty("description").GetString();
        await Assert.That(description).Contains("uses raw message delivery");
        await Assert.That(description).DoesNotContain("notification envelope");
    }

    [Test]
    public async Task APointToPointSubscriptionHasNoDeliveryModeDescription()
    {
        using var document = await GenerateAsync(rawMessageDelivery: false);
        var root = document.RootElement;

        // The SQS body of a point-to-point queue is the payload itself; there is no envelope to call out.
        var operation = root.GetProperty("operations").GetProperty("receive-orderdispatched");
        await Assert.That(operation.TryGetProperty("description", out _)).IsFalse();
    }
}
