using Amazon.SQS.Model;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// Proves a CloudEvent can be consumed as its bare <c>data</c> payload per registration: the handler
/// is a plain <c>IHandlerAsync&lt;T&gt;</c> with no CloudEvents in its contract, the CloudEvents
/// <c>type</c> is stated once at the subscription (routing the message and selecting its serializer),
/// and neither the app-wide serializer nor the CloudEvents options type map is involved.
/// </summary>
public class WhenHandlingCloudEventData : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task Then_The_Handler_Receives_The_Bare_Payload()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        handler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call => completionSource.TrySetResult(call.Arg<OrderPlaced>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .HandlingCloudEventData<OrderPlaced>(OrderPlacedType))))
            .AddSingleton(handler);

        // Consume-only CloudEvents support: no source, no type map - the `type` above is enough.
        services.AddJustSayingCloudEvents();

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);

                // Act - drop a raw structured CloudEvent (as another system would produce it) onto the queue.
                var sqs = CreateClientFactory().GetSqsClient(Region);
                var queueUrl = (await sqs.GetQueueUrlAsync(UniqueName, cancellationToken)).QueueUrl;
                var envelope = $$"""
                    {
                      "specversion": "1.0",
                      "id": "{{Guid.NewGuid()}}",
                      "source": "https://orders.example.com",
                      "type": "{{OrderPlacedType}}",
                      "time": "{{DateTimeOffset.UtcNow:O}}",
                      "datacontenttype": "application/json",
                      "data": { "OrderId": "order-7" }
                    }
                    """;
                await sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = queueUrl, MessageBody = envelope }, cancellationToken);

                // Assert - the envelope was stripped and the handler received the payload alone.
                (await completionSource.Task.WaitAsync(cancellationToken)).OrderId.ShouldBe("order-7");
            });
    }
}
