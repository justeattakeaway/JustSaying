using System.Text.Json;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// The destination overloads register two publications — the bare model and the
/// <see cref="CloudEvent{T}"/> envelope — and a <see cref="QueueDestination.ByConvention()"/> or
/// <see cref="TopicDestination.ByConvention()"/> destination is resolved once per registration. These
/// tests pin the dual-registration guarantee: both shapes resolve to the destination named after the
/// payload type, not the <c>CloudEvent&lt;T&gt;</c> wrapper, so only one queue or topic exists.
/// </summary>
public class WhenPublishingCloudEventsByConvention : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";
    private static readonly Uri RegistrationSource = new("https://orders.example.com");

    // The default naming convention lowercases the type name, so these are "conventionorderplaced"
    // and — if the envelope registration were named after the wrapper — "cloudeventconventionorderplaced".
    private const string ConventionName = "conventionorderplaced";
    private const string WrapperConventionName = "cloudeventconventionorderplaced";

    public sealed class ConventionOrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task Then_Both_Shapes_Publish_To_The_Queue_Named_After_The_Payload_Type()
    {
        // Arrange
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventQueue<ConventionOrderPlaced>(
                    QueueDestination.ByConvention(), OrderPlacedType, RegistrationSource)));

        services.AddJustSayingCloudEvents();

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        await RunActionWithTimeout(async cancellationToken =>
        {
            await publisher.StartAsync(cancellationToken); // creates the queue(s)

            // Act
            await publisher.PublishAsync(new ConventionOrderPlaced { OrderId = "bare-1" }, cancellationToken);
            await publisher.PublishAsync(new CloudEvent<ConventionOrderPlaced>(
                new ConventionOrderPlaced { OrderId = "wrapped-2" },
                subject: "orders/2"), cancellationToken);

            // Assert - both shapes are on the one queue named for the payload type...
            var sqs = CreateClientFactory().GetSqsClient(Region);
            var queueUrl = (await sqs.GetQueueUrlAsync(ConventionName, cancellationToken)).QueueUrl;
            var bodies = await ReceiveManyAsync(sqs, queueUrl, 2, cancellationToken);

            bodies.Count.ShouldBe(2);
            OrderIds(bodies).ShouldBe(["bare-1", "wrapped-2"], ignoreOrder: true);

            // ...and the envelope registration never created a queue of its own.
            await Should.ThrowAsync<QueueDoesNotExistException>(
                () => sqs.GetQueueUrlAsync(WrapperConventionName, cancellationToken));
        });
    }

    [Test]
    public async Task Then_Both_Shapes_Publish_To_The_Topic_Named_After_The_Payload_Type()
    {
        // Arrange
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventTopic<ConventionOrderPlaced>(
                    TopicDestination.ByConvention(), OrderPlacedType, RegistrationSource)));

        services.AddJustSayingCloudEvents();

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        var sns = CreateClientFactory().GetSnsClient(Region);
        var sqs = CreateClientFactory().GetSqsClient(Region);

        await RunActionWithTimeout(async cancellationToken =>
        {
            await publisher.StartAsync(cancellationToken); // creates the topic(s)

            // The envelope registration must not have created a topic of its own.
            (await sns.FindTopicAsync(WrapperConventionName)).ShouldBeNull();

            var topic = await sns.FindTopicAsync(ConventionName);
            topic.ShouldNotBeNull();

            // Capture what the one topic receives.
            var queueUrl = (await sqs.CreateQueueAsync(new CreateQueueRequest { QueueName = UniqueName + "-capture" }, cancellationToken)).QueueUrl;
            var queueArn = (await sqs.GetQueueAttributesAsync(
                new GetQueueAttributesRequest { QueueUrl = queueUrl, AttributeNames = ["QueueArn"] }, cancellationToken)).Attributes["QueueArn"];
            var subscriptionArn = (await sns.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = topic.TopicArn,
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

            // Act
            await publisher.PublishAsync(new ConventionOrderPlaced { OrderId = "bare-1" }, cancellationToken);
            await publisher.PublishAsync(new CloudEvent<ConventionOrderPlaced>(
                new ConventionOrderPlaced { OrderId = "wrapped-2" },
                subject: "orders/2"), cancellationToken);

            // Assert - both shapes arrived through the same topic.
            var bodies = await ReceiveManyAsync(sqs, queueUrl, 2, cancellationToken);

            bodies.Count.ShouldBe(2);
            OrderIds(bodies).ShouldBe(["bare-1", "wrapped-2"], ignoreOrder: true);
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

    private static List<string> OrderIds(List<string> bodies)
    {
        var orderIds = new List<string>();
        foreach (var body in bodies)
        {
            using var document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("type").GetString().ShouldBe(OrderPlacedType);
            orderIds.Add(document.RootElement.GetProperty("data").GetProperty("OrderId").GetString());
        }

        return orderIds;
    }
}
