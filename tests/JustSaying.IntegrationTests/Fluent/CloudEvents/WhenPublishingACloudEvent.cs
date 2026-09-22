using System.Text.Json;
using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// End-to-end coverage for CloudEvents support: a message configured for CloudEvents serialization
/// round-trips through the full in-memory publish -> subscribe -> handle path, and the body placed on
/// the wire is a structured-mode CloudEvents 1.0 envelope.
/// </summary>
public class WhenPublishingACloudEvent : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    private IServiceCollection GivenCloudEvents(IServiceCollection services)
    {
        // GivenJustSaying() has already registered the default System.Text.Json serialization
        // factory; replace it with the CloudEvents factory (which AddJustSayingCloudEvents only
        // registers via TryAdd).
        services.RemoveAll<IMessageBodySerializationFactory>();
        services.AddJustSayingCloudEvents(options =>
        {
            options.Source = new Uri("https://orders.example.com");
            options.WithCloudEventType<OrderPlaced>(OrderPlacedType);
        });

        return services;
    }

    [Test]
    public async Task Then_The_Message_Round_Trips()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        handler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call => completionSource.TrySetResult(call.Arg<OrderPlaced>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithQueue<OrderPlaced>(o => o.WithQueueName(UniqueName)))
                .Subscriptions(s => s.ForQueue<OrderPlaced>(sub => sub.WithQueueName(UniqueName))))
            .AddSingleton(handler);

        GivenCloudEvents(services);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new OrderPlaced { OrderId = "order-42" }, cancellationToken);

                // Assert - the data payload was unwrapped from the CloudEvents envelope and handled.
                var handled = await completionSource.Task.WaitAsync(cancellationToken);
                handled.OrderId.ShouldBe("order-42");
            });
    }

    [Test]
    public async Task Then_The_Wire_Body_Is_A_Structured_CloudEvent()
    {
        // Arrange
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithQueue<OrderPlaced>(o => o.WithQueueName(UniqueName))));

        GivenCloudEvents(services);

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<Messaging.IMessagePublisher>();

        await RunActionWithTimeout(async cancellationToken =>
        {
            await publisher.StartAsync(cancellationToken);

            // Act
            await publisher.PublishAsync(new OrderPlaced { OrderId = "order-42" }, cancellationToken);

            // Assert - read the raw message straight off the queue and inspect the envelope.
            var sqs = CreateClientFactory().GetSqsClient(Region);
            var queueUrl = (await sqs.GetQueueUrlAsync(UniqueName, cancellationToken)).QueueUrl;
            var received = await sqs.ReceiveMessageAsync(
                new ReceiveMessageRequest { QueueUrl = queueUrl, MaxNumberOfMessages = 1, WaitTimeSeconds = 1 },
                cancellationToken);

            received.Messages.ShouldHaveSingleItem();

            // The CloudEvents serializer is self-describing, so JustSaying publishes it without its
            // {"Message", "Subject"} queue envelope: the body on the wire IS the structured CloudEvent.
            using var document = JsonDocument.Parse(received.Messages[0].Body);
            var root = document.RootElement;

            root.TryGetProperty("Message", out _).ShouldBeFalse("the CloudEvent should not be double-wrapped");
            root.GetProperty("specversion").GetString().ShouldBe("1.0");
            root.GetProperty("type").GetString().ShouldBe(OrderPlacedType);
            root.GetProperty("source").GetString().ShouldBe("https://orders.example.com/");
            root.GetProperty("datacontenttype").GetString().ShouldBe("application/json");
            root.GetProperty("id").GetString().ShouldNotBeNullOrEmpty();
            root.TryGetProperty("time", out _).ShouldBeTrue();
            root.GetProperty("data").GetProperty("OrderId").GetString().ShouldBe("order-42");
        });
    }
}
