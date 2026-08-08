using System.Text.Json;
using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// The point-to-point counterpart of <see cref="WhenPublishingACloudEventEnvelope"/>: a single
/// <c>WithCloudEventQueue&lt;T&gt;</c> publication accepts both shapes — the bare model and a
/// <see cref="CloudEvent{T}"/> — and, because the CloudEvents serializer is self-describing, the
/// structured envelope is the body on the wire, never wrapped in JustSaying's
/// <c>{ "Subject", "Message" }</c> queue envelope.
/// </summary>
public class WhenPublishingACloudEventToAQueue : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";
    private static readonly Uri RegistrationSource = new("https://orders.example.com");

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task Then_Both_Shapes_Publish_Through_One_Registration()
    {
        // Arrange
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventQueue<OrderPlaced>(OrderPlacedType, source: RegistrationSource, queueName: UniqueName)));

        services.AddJustSayingCloudEvents();

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        await RunActionWithTimeout(async cancellationToken =>
        {
            await publisher.StartAsync(cancellationToken); // creates the queue

            // Act - the same registration accepts the bare model and the envelope.
            await publisher.PublishAsync(new OrderPlaced { OrderId = "bare-1" }, cancellationToken);
            await publisher.PublishAsync(new CloudEvent<OrderPlaced>(
                new OrderPlaced { OrderId = "wrapped-2" },
                source: new Uri("https://orders.example.com/eu"),
                subject: "orders/2",
                extensions: new Dictionary<string, string> { ["tenantid"] = "acme" }), cancellationToken);

            // Assert - read the raw messages straight off the queue.
            var sqs = CreateClientFactory().GetSqsClient(Region);
            var queueUrl = (await sqs.GetQueueUrlAsync(UniqueName, cancellationToken)).QueueUrl;
            var bodies = await ReceiveManyAsync(sqs, queueUrl, 2, cancellationToken);
            bodies.Count.ShouldBe(2);

            var bare = ParseByOrderId(bodies, "bare-1");
            bare.TryGetProperty("Message", out _).ShouldBeFalse("the CloudEvent should not be double-wrapped");
            bare.GetProperty("specversion").GetString().ShouldBe("1.0");
            bare.GetProperty("type").GetString().ShouldBe(OrderPlacedType);
            bare.GetProperty("source").GetString().ShouldBe(RegistrationSource.ToString());
            bare.GetProperty("id").GetString().ShouldNotBeNullOrEmpty();
            bare.TryGetProperty("subject", out _).ShouldBeFalse();

            var wrapped = ParseByOrderId(bodies, "wrapped-2");
            wrapped.TryGetProperty("Message", out _).ShouldBeFalse("the CloudEvent should not be double-wrapped");
            wrapped.GetProperty("type").GetString().ShouldBe(OrderPlacedType);
            wrapped.GetProperty("source").GetString().ShouldBe("https://orders.example.com/eu");
            wrapped.GetProperty("subject").GetString().ShouldBe("orders/2");
            wrapped.GetProperty("tenantid").GetString().ShouldBe("acme");
        });
    }

    [Test]
    public void Then_Building_The_Bus_Throws_When_No_Source_Is_Configured()
    {
        // Arrange - no source on the publication and none on CloudEventOptions.
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventQueue<OrderPlaced>(OrderPlacedType, queueName: UniqueName)));

        services.AddJustSayingCloudEvents();

        var serviceProvider = services.BuildServiceProvider();

        // Act and Assert - fails at bus build, not on first publish.
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagePublisher>());
        exception.Message.ShouldContain("source");
    }

    private static async Task<List<string>> ReceiveManyAsync(Amazon.SQS.IAmazonSQS sqs, string queueUrl, int count, CancellationToken cancellationToken)
    {
        var bodies = new List<string>();
        for (var i = 0; i < 20 && bodies.Count < count; i++)
        {
            var response = await sqs.ReceiveMessageAsync(
                new ReceiveMessageRequest { QueueUrl = queueUrl, MaxNumberOfMessages = 10, WaitTimeSeconds = 1 }, cancellationToken);
            foreach (var message in response.Messages ?? [])
            {
                bodies.Add(message.Body);
                await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
            }
        }

        return bodies;
    }

    private static JsonElement ParseByOrderId(List<string> bodies, string orderId)
    {
        foreach (var body in bodies)
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("data", out var data)
                && data.GetProperty("OrderId").GetString() == orderId)
            {
                return document.RootElement.Clone();
            }
        }

        throw new ShouldAssertException($"No published CloudEvent had data.OrderId == '{orderId}'.");
    }
}
