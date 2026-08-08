using System.Text.Json;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// Proves the CloudEvents publications compose with pre-existing infrastructure: the same
/// <c>WithCloudEventTopic&lt;T&gt;</c> / <c>WithCloudEventQueue&lt;T&gt;</c> registrations accept a
/// <see cref="TopicAddress"/> / <see cref="QueueAddress"/>, publish both shapes to the addressed
/// resource, and never create anything.
/// </summary>
public class WhenPublishingCloudEventsToExistingInfrastructure : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";
    private static readonly Uri RegistrationSource = new("https://orders.example.com");

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task Then_Both_Shapes_Publish_To_An_Existing_Topic_By_Arn()
    {
        // Arrange - the topic and a raw-delivery capture queue exist before JustSaying is configured.
        var sns = CreateClientFactory().GetSnsClient(Region);
        var sqs = CreateClientFactory().GetSqsClient(Region);

        var topicArn = (await sns.CreateTopicAsync(new CreateTopicRequest { Name = UniqueName })).TopicArn;
        var queueUrl = (await sqs.CreateQueueAsync(new CreateQueueRequest { QueueName = UniqueName + "-capture" })).QueueUrl;
        var queueArn = (await sqs.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { QueueUrl = queueUrl, AttributeNames = ["QueueArn"] })).Attributes["QueueArn"];
        var subscriptionArn = (await sns.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn,
            ReturnSubscriptionArn = true,
        })).SubscriptionArn;
        await sns.SetSubscriptionAttributesAsync(new SetSubscriptionAttributesRequest
        {
            SubscriptionArn = subscriptionArn,
            AttributeName = "RawMessageDelivery",
            AttributeValue = "true",
        });

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventTopic<OrderPlaced>(
                    TopicAddress.FromArn(topicArn), OrderPlacedType, RegistrationSource)));

        services.AddJustSayingCloudEvents();

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        await RunActionWithTimeout(async cancellationToken =>
        {
            await publisher.StartAsync(cancellationToken);

            // Act - the same registration accepts the bare model and the envelope.
            await publisher.PublishAsync(new OrderPlaced { OrderId = "bare-1" }, cancellationToken);
            await publisher.PublishAsync(new CloudEvent<OrderPlaced>(
                new OrderPlaced { OrderId = "wrapped-2" },
                subject: "orders/2"), cancellationToken);

            // Assert
            var bodies = await ReceiveManyAsync(sqs, queueUrl, 2, cancellationToken);
            bodies.Count.ShouldBe(2);

            var bare = ParseByOrderId(bodies, "bare-1");
            bare.GetProperty("specversion").GetString().ShouldBe("1.0");
            bare.GetProperty("type").GetString().ShouldBe(OrderPlacedType);
            bare.GetProperty("source").GetString().ShouldBe(RegistrationSource.ToString());

            var wrapped = ParseByOrderId(bodies, "wrapped-2");
            wrapped.GetProperty("type").GetString().ShouldBe(OrderPlacedType);
            wrapped.GetProperty("subject").GetString().ShouldBe("orders/2");
        });
    }

    [Test]
    public async Task Then_Both_Shapes_Publish_To_An_Existing_Queue_By_Url()
    {
        // Arrange - the queue exists before JustSaying is configured.
        var sqs = CreateClientFactory().GetSqsClient(Region);
        var queueUrl = (await sqs.CreateQueueAsync(new CreateQueueRequest { QueueName = UniqueName })).QueueUrl;

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventQueue<OrderPlaced>(
                    QueueAddress.FromUrl(queueUrl), OrderPlacedType, RegistrationSource)));

        services.AddJustSayingCloudEvents();

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        await RunActionWithTimeout(async cancellationToken =>
        {
            await publisher.StartAsync(cancellationToken);

            // Act
            await publisher.PublishAsync(new OrderPlaced { OrderId = "bare-1" }, cancellationToken);
            await publisher.PublishAsync(new CloudEvent<OrderPlaced>(
                new OrderPlaced { OrderId = "wrapped-2" },
                subject: "orders/2"), cancellationToken);

            // Assert - the structured envelope is the queue body verbatim, not double-wrapped.
            var bodies = await ReceiveManyAsync(sqs, queueUrl, 2, cancellationToken);
            bodies.Count.ShouldBe(2);

            var bare = ParseByOrderId(bodies, "bare-1");
            bare.TryGetProperty("Message", out _).ShouldBeFalse("the CloudEvent should not be double-wrapped");
            bare.GetProperty("type").GetString().ShouldBe(OrderPlacedType);
            bare.GetProperty("source").GetString().ShouldBe(RegistrationSource.ToString());

            var wrapped = ParseByOrderId(bodies, "wrapped-2");
            wrapped.GetProperty("subject").GetString().ShouldBe("orders/2");
        });
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
