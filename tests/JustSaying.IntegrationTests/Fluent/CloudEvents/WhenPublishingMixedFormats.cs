using System.Text.Json;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// Proves the envelope is chosen per publication, not per application: one app publishes a legacy
/// <see cref="Message"/>-derived type, a plain JSON POCO and a CloudEvents type side by side. Adding
/// CloudEvents support leaves the non-CloudEvents publications on the app-wide default serializer —
/// no factory replacement, no registration-order sensitivity.
/// </summary>
public class WhenPublishingMixedFormats : IntegrationTestBase
{
    private const string ParcelShippedType = "com.example.parcels.parcel.shipped";
    private static readonly Uri ParcelSource = new("https://parcels.example.com");

    public sealed class OrderPlaced : JustSaying.Models.Message
    {
        public string OrderId { get; set; }
    }

    public sealed class PaymentTaken
    {
        public string PaymentId { get; set; }
    }

    public sealed class ParcelShipped
    {
        public string ParcelId { get; set; }
    }

    [Test]
    public async Task Then_Legacy_Plain_And_CloudEvents_Publications_Coexist()
    {
        // Arrange
        var parcelTopic = UniqueName + "-parcels";

        var orderHandled = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);
        var paymentHandled = new TaskCompletionSource<PaymentTaken>(TaskCreationOptions.RunContinuationsAsynchronously);

        var orderHandler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        orderHandler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call => orderHandled.TrySetResult(call.Arg<OrderPlaced>()));

        var paymentHandler = Substitute.For<IHandlerAsync<PaymentTaken>>();
        paymentHandler.Handle(Arg.Any<PaymentTaken>())
            .Returns(true)
            .AndDoes(call => paymentHandled.TrySetResult(call.Arg<PaymentTaken>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p =>
                {
                    p.WithTopic<OrderPlaced>();                        // legacy — derives from Message
                    p.WithTopic<PaymentTaken>();                       // plain JSON POCO
                    p.WithCloudEventTopic<ParcelShipped>(ParcelShippedType, // CloudEvents
                        source: ParcelSource,
                        topicName: parcelTopic);
                })
                .Subscriptions(s =>
                {
                    s.ForTopic<OrderPlaced>(c => c.WithQueueName(UniqueName + "-legacy"));
                    s.ForTopic<PaymentTaken>(c => c.WithQueueName(UniqueName + "-plain"));
                }))
            .AddSingleton(orderHandler)
            .AddSingleton(paymentHandler);

        // No useAsDefault: only the WithCloudEventTopic publication speaks CloudEvents.
        services.AddJustSayingCloudEvents();

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                var (sqs, queueUrl) = await SubscribeCaptureQueueAsync(parcelTopic, cancellationToken);

                // Act - all three formats through one publisher, plus the CloudEvents envelope shape.
                await publisher.PublishAsync(new OrderPlaced { OrderId = "order-1" }, cancellationToken);
                await publisher.PublishAsync(new PaymentTaken { PaymentId = "payment-2" }, cancellationToken);
                await publisher.PublishAsync(new ParcelShipped { ParcelId = "parcel-3" }, cancellationToken);
                await publisher.PublishAsync(new CloudEvent<ParcelShipped>(
                    new ParcelShipped { ParcelId = "parcel-4" },
                    subject: "parcels/4"), cancellationToken);

                // Assert - the legacy and plain messages round-trip through their own subscriptions.
                (await orderHandled.Task.WaitAsync(cancellationToken)).OrderId.ShouldBe("order-1");
                (await paymentHandled.Task.WaitAsync(cancellationToken)).PaymentId.ShouldBe("payment-2");

                // Assert - both CloudEvents shapes reached the parcel topic as structured CloudEvents.
                var notifications = await ReceiveManyAsync(sqs, queueUrl, 2, cancellationToken);
                notifications.Count.ShouldBe(2);

                var bare = FindByParcelId(notifications, "parcel-3");
                bare.CloudEvent.GetProperty("specversion").GetString().ShouldBe("1.0");
                bare.CloudEvent.GetProperty("type").GetString().ShouldBe(ParcelShippedType);
                bare.CloudEvent.GetProperty("source").GetString().ShouldBe(ParcelSource.ToString());

                var wrapped = FindByParcelId(notifications, "parcel-4");
                wrapped.CloudEvent.GetProperty("type").GetString().ShouldBe(ParcelShippedType);
                wrapped.CloudEvent.GetProperty("subject").GetString().ShouldBe("parcels/4");

                // The SNS Subject reflects the payload type for both shapes - not "CloudEvent`1".
                bare.SnsSubject.ShouldBe(nameof(ParcelShipped));
                wrapped.SnsSubject.ShouldBe(nameof(ParcelShipped));
            });
    }

    private async Task<(Amazon.SQS.IAmazonSQS Sqs, string QueueUrl)> SubscribeCaptureQueueAsync(string topicName, CancellationToken cancellationToken)
    {
        // Wire a queue to the topic without raw delivery, so the SNS notification (and its Subject)
        // can be read off the wire alongside the CloudEvents body.
        var sns = CreateClientFactory().GetSnsClient(Region);
        var sqs = CreateClientFactory().GetSqsClient(Region);

        var topicArn = (await sns.CreateTopicAsync(new CreateTopicRequest { Name = topicName }, cancellationToken)).TopicArn;
        var queueUrl = (await sqs.CreateQueueAsync(new CreateQueueRequest { QueueName = topicName + "-capture" }, cancellationToken)).QueueUrl;
        var queueArn = (await sqs.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { QueueUrl = queueUrl, AttributeNames = ["QueueArn"] }, cancellationToken)).Attributes["QueueArn"];
        await sns.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn,
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

    private static (JsonElement CloudEvent, string SnsSubject) FindByParcelId(List<string> notifications, string parcelId)
    {
        foreach (var notification in notifications)
        {
            using var document = JsonDocument.Parse(notification);
            var root = document.RootElement;
            var subject = root.TryGetProperty("Subject", out var subjectElement) ? subjectElement.GetString() : null;

            using var cloudEvent = JsonDocument.Parse(root.GetProperty("Message").GetString()!);
            if (cloudEvent.RootElement.GetProperty("data").GetProperty("ParcelId").GetString() == parcelId)
            {
                return (cloudEvent.RootElement.Clone(), subject);
            }
        }

        throw new ShouldAssertException($"No captured CloudEvent had data.ParcelId == '{parcelId}'.");
    }
}
