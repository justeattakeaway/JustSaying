using System.Collections.Concurrent;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// The native → CloudEvents migration path: a producer moves a type from a native topic publication to
/// <c>WithCloudEventTopic&lt;T&gt;</c>, and the consumer's queue accepts both shapes during the rollout
/// with <c>Handling&lt;T&gt;()</c> plus <c>HandlingCloudEventData&lt;T&gt;(type)</c>. A CloudEvents topic
/// publication carries the payload's type name as its SNS Subject, so the queue's routing must not let
/// the Subject read a CloudEvent as a native message — whatever order things are registered in.
/// </summary>
public class WhenMigratingATypeToCloudEvents : IntegrationTestBase
{
    private const string ParcelShippedType = "com.example.parcel-shipped";

    public sealed class ParcelShipped
    {
        public string Tracking { get; set; }
    }

    [Test]
    [Arguments("native-first")]
    [Arguments("cloudevents-first")]
    [Arguments("subject-discriminator-first")]
    [Arguments("subject-discriminator-last")]
    public async Task Then_Both_Shapes_Are_Read_Correctly_In_Any_Registration_Order(string order)
    {
        // Arrange
        var queueName = UniqueName + "-consumer";
        var handled = new ConcurrentQueue<ParcelShipped>();
        var bothHandled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = Substitute.For<IHandlerAsync<ParcelShipped>>();
        handler.Handle(Arg.Any<ParcelShipped>())
            .Returns(true)
            .AndDoes(call =>
            {
                handled.Enqueue(call.Arg<ParcelShipped>());
                if (handled.Count == 2)
                {
                    bothHandled.TrySetResult();
                }
            });

        var consumer = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(queueName, q =>
                {
                    switch (order)
                    {
                        case "native-first":
                            q.Handling<ParcelShipped>().HandlingCloudEventData<ParcelShipped>(ParcelShippedType);
                            break;
                        case "cloudevents-first":
                            q.HandlingCloudEventData<ParcelShipped>(ParcelShippedType).Handling<ParcelShipped>();
                            break;
                        case "subject-discriminator-first":
                            q.WithDiscriminator(new SubjectMessageTypeDiscriminator())
                                .Handling<ParcelShipped>()
                                .HandlingCloudEventData<ParcelShipped>(ParcelShippedType);
                            break;
                        default:
                            q.Handling<ParcelShipped>()
                                .HandlingCloudEventData<ParcelShipped>(ParcelShippedType)
                                .WithDiscriminator(new SubjectMessageTypeDiscriminator());
                            break;
                    }
                })))
            .AddSingleton(handler);

        consumer.AddJustSayingCloudEvents();

        // Two producers on the same topic: the old native publication, and the new CloudEvents one.
        var nativeProducer = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithTopic<ParcelShipped>(t => t.WithTopicName(UniqueName))));

        var cloudEventsProducer = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventTopic<ParcelShipped>(
                    ParcelShippedType, source: new Uri("/parcels", UriKind.Relative), topicName: UniqueName)));

        cloudEventsProducer.AddJustSayingCloudEvents();

        var nativePublisher = nativeProducer.BuildServiceProvider().GetRequiredService<IMessagePublisher>();
        var cloudEventsPublisher = cloudEventsProducer.BuildServiceProvider().GetRequiredService<IMessagePublisher>();

        await WhenAsync(
            consumer,
            async (_, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await nativePublisher.StartAsync(cancellationToken);
                await cloudEventsPublisher.StartAsync(cancellationToken);

                // The multi-type queue has no fluent topic subscription; wire it to the topic directly.
                await SubscribeQueueToTopicAsync(queueName, cancellationToken);

                // Act
                await nativePublisher.PublishAsync(new ParcelShipped { Tracking = "native-1" }, cancellationToken);
                await cloudEventsPublisher.PublishAsync(new ParcelShipped { Tracking = "cloudevent-1" }, cancellationToken);

                // Assert - each shape went through its own serializer, so neither arrived with default fields.
                await bothHandled.Task.WaitAsync(cancellationToken);
                handled.Select(message => message.Tracking).OrderBy(tracking => tracking, StringComparer.Ordinal)
                    .ShouldBe(["cloudevent-1", "native-1"]);
            });
    }

    private async Task SubscribeQueueToTopicAsync(string queueName, CancellationToken cancellationToken)
    {
        var sns = CreateClientFactory().GetSnsClient(Region);
        var sqs = CreateClientFactory().GetSqsClient(Region);

        var topicArn = (await sns.CreateTopicAsync(new CreateTopicRequest { Name = UniqueName }, cancellationToken)).TopicArn;
        var queueUrl = (await sqs.GetQueueUrlAsync(queueName, cancellationToken)).QueueUrl;
        var queueArn = (await sqs.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { QueueUrl = queueUrl, AttributeNames = ["QueueArn"] }, cancellationToken)).Attributes["QueueArn"];

        await sns.SubscribeAsync(new SubscribeRequest { TopicArn = topicArn, Protocol = "sqs", Endpoint = queueArn }, cancellationToken);
    }
}
