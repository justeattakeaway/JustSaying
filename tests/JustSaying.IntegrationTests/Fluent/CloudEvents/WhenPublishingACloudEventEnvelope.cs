using System.Text.Json;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// The publish-side counterpart of <see cref="WhenHandlingACloudEventEnvelope"/>: a single
/// <c>WithCloudEvent&lt;T&gt;</c> publication (type, source and topic co-located, no global type map)
/// accepts both shapes — the bare model, whose envelope metadata is defaulted, and a
/// <see cref="CloudEvent{T}"/>, whose <c>source</c>, <c>subject</c> and extension attributes are set
/// per message.
/// </summary>
public class WhenPublishingACloudEventEnvelope : IntegrationTestBase
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
                .Publications(p => p.WithCloudEvent<OrderPlaced>(OrderPlacedType, source: RegistrationSource, topicName: UniqueName)));

        services.RemoveAll<IMessageBodySerializationFactory>();
        services.AddJustSayingCloudEvents();

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        await RunActionWithTimeout(async cancellationToken =>
        {
            await publisher.StartAsync(cancellationToken); // creates the topic

            var (sqs, queueUrl) = await SubscribeCaptureQueueAsync(cancellationToken);

            // Act - the same registration accepts the bare model and the envelope.
            await publisher.PublishAsync(new OrderPlaced { OrderId = "bare-1" }, cancellationToken);
            await publisher.PublishAsync(new CloudEvent<OrderPlaced>(
                new OrderPlaced { OrderId = "wrapped-2" },
                source: new Uri("https://orders.example.com/eu"),
                subject: "orders/2",
                extensions: new Dictionary<string, string> { ["tenantid"] = "acme" }), cancellationToken);

            // Assert
            var bodies = await ReceiveManyAsync(sqs, queueUrl, 2, cancellationToken);
            bodies.Count.ShouldBe(2);

            var bare = ParseByOrderId(bodies, "bare-1");
            bare.GetProperty("specversion").GetString().ShouldBe("1.0");
            bare.GetProperty("type").GetString().ShouldBe(OrderPlacedType);
            bare.GetProperty("source").GetString().ShouldBe(RegistrationSource.ToString());
            bare.GetProperty("id").GetString().ShouldNotBeNullOrEmpty();
            bare.TryGetProperty("subject", out _).ShouldBeFalse();

            var wrapped = ParseByOrderId(bodies, "wrapped-2");
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
                .Publications(p => p.WithCloudEvent<OrderPlaced>(OrderPlacedType, topicName: UniqueName)));

        services.RemoveAll<IMessageBodySerializationFactory>();
        services.AddJustSayingCloudEvents();

        var serviceProvider = services.BuildServiceProvider();

        // Act and Assert - fails at bus build, not on first publish.
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagePublisher>());
        exception.Message.ShouldContain("source");
    }

    [Test]
    public void Then_Building_The_Bus_Throws_When_The_Same_Type_Has_Two_Publications()
    {
        // Arrange - a plain topic publication and a CloudEvents publication for the same type.
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p =>
                {
                    p.WithTopic<OrderPlaced>(t => t.WithTopicName(UniqueName));
                    p.WithCloudEvent<OrderPlaced>(OrderPlacedType, source: RegistrationSource, topicName: UniqueName);
                }));

        services.RemoveAll<IMessageBodySerializationFactory>();
        services.AddJustSayingCloudEvents(options =>
        {
            options.Source = RegistrationSource;
            options.WithCloudEventType<OrderPlaced>(OrderPlacedType);
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act and Assert - the silent last-write-wins is now a startup error.
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagePublisher>());
        exception.Message.ShouldContain("already registered");
    }

    private async Task<(Amazon.SQS.IAmazonSQS Sqs, string QueueUrl)> SubscribeCaptureQueueAsync(CancellationToken cancellationToken)
    {
        // Wire a queue to the topic with raw delivery so the bare envelope can be read off the wire.
        var sns = CreateClientFactory().GetSnsClient(Region);
        var sqs = CreateClientFactory().GetSqsClient(Region);

        var topicArn = (await sns.CreateTopicAsync(new CreateTopicRequest { Name = UniqueName }, cancellationToken)).TopicArn;
        var queueUrl = (await sqs.CreateQueueAsync(new CreateQueueRequest { QueueName = UniqueName + "-capture" }, cancellationToken)).QueueUrl;
        var queueArn = (await sqs.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { QueueUrl = queueUrl, AttributeNames = ["QueueArn"] }, cancellationToken)).Attributes["QueueArn"];
        var subscriptionArn = (await sns.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn,
            ReturnSubscriptionArn = true,
        }, cancellationToken)).SubscriptionArn;
        await sns.SetSubscriptionAttributesAsync(new SetSubscriptionAttributesRequest
        {
            SubscriptionArn = subscriptionArn,
            AttributeName = "RawMessageDelivery",
            AttributeValue = "true",
        }, cancellationToken);

        return (sqs, queueUrl);
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
            if (document.RootElement.GetProperty("data").GetProperty("OrderId").GetString() == orderId)
            {
                return document.RootElement.Clone();
            }
        }

        throw new ShouldAssertException($"No published CloudEvent had data.OrderId == '{orderId}'.");
    }
}
