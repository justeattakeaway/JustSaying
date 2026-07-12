using System.Text;
using System.Text.Json;
using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// Proves a handler can opt into the CloudEvents envelope by handling <see cref="CloudEvent{T}"/>: it
/// receives the envelope metadata (<c>source</c>, <c>id</c>, <c>type</c>) and extension attributes
/// alongside the deserialized <c>data</c>, instead of just the payload.
/// </summary>
public class WhenHandlingACloudEventEnvelope : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task Then_The_Handler_Receives_The_Envelope_Metadata_And_Extensions()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<CloudEvent<OrderPlaced>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = Substitute.For<IHandlerAsync<CloudEvent<OrderPlaced>>>();
        handler.Handle(Arg.Any<CloudEvent<OrderPlaced>>())
            .Returns(true)
            .AndDoes(call => completionSource.TrySetResult(call.Arg<CloudEvent<OrderPlaced>>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                // A consume-only CloudEvents subscription: the type is stated here (co-located with the
                // handler), the CloudEvents discriminator is added automatically, and no global
                // CloudEvents configuration (source / type map) is needed.
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .HandlingCloudEvent<OrderPlaced>(OrderPlacedType))))
            .AddSingleton(handler);

        services.RemoveAll<IMessageBodySerializationFactory>();
        services.AddJustSayingCloudEvents();

        var id = Guid.NewGuid().ToString();
        var envelope = BuildCloudEvent(id, "https://orders.example.com/region/eu", OrderPlacedType, tenantId: "acme", orderId: "order-99");

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);

                // Act - drop a raw structured CloudEvent (with an extension attribute) onto the queue.
                var sqs = CreateClientFactory().GetSqsClient(Region);
                var queueUrl = (await sqs.GetQueueUrlAsync(UniqueName, cancellationToken)).QueueUrl;
                await sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = queueUrl, MessageBody = envelope }, cancellationToken);

                // Assert
                var received = await completionSource.Task.WaitAsync(cancellationToken);
                received.Data.OrderId.ShouldBe("order-99");
                received.Id.ShouldBe(id);
                received.Type.ShouldBe(OrderPlacedType);
                received.Source.ShouldBe(new Uri("https://orders.example.com/region/eu"));
                received.Extensions.ShouldContainKeyAndValue("tenantid", "acme");
            });
    }

    private static string BuildCloudEvent(string id, string source, string type, string tenantId, string orderId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("specversion", "1.0");
            writer.WriteString("id", id);
            writer.WriteString("source", source);
            writer.WriteString("type", type);
            writer.WriteString("time", DateTimeOffset.UtcNow);
            writer.WriteString("datacontenttype", "application/json");
            writer.WriteString("tenantid", tenantId); // a CloudEvents extension attribute
            writer.WritePropertyName("data");
            writer.WriteStartObject();
            writer.WriteString("OrderId", orderId);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
