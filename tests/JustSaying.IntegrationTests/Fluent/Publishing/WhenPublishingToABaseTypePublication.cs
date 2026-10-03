using System.Text.Json.Serialization;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

/// <summary>
/// When no publication is registered for a message's runtime type, the message is published to the
/// publication registered for its closest base class (or an interface it implements), serialized as
/// that type. With a <c>[JsonPolymorphic]</c> base type this writes the discriminator, so a consumer
/// of the base type can deserialize the derived message.
/// </summary>
public class WhenPublishingToABaseTypePublication : IntegrationTestBase
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(StockReserved), "reserved")]
    [JsonDerivedType(typeof(StockReleased), "released")]
    public abstract class WarehouseEvent
    {
        public string Sku { get; set; }
    }

    public sealed class StockReserved : WarehouseEvent
    {
        public int Quantity { get; set; }
    }

    public sealed class StockReleased : WarehouseEvent
    {
        public string Reason { get; set; }
    }

    [Test]
    public async Task Then_A_Base_Type_Subscriber_Receives_The_Derived_Message()
    {
        // Arrange
        var handler = new InspectableHandler<WarehouseEvent>();

        var services = GivenSystemTextJson()
            .ConfigureJustSaying(builder => builder
                .Publications(pub => pub.WithTopic<WarehouseEvent>())
                .Subscriptions(sub => sub.ForTopic<WarehouseEvent>(c => c.WithQueueName(UniqueName))))
            .AddSingleton<IHandlerAsync<WarehouseEvent>>(handler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new StockReserved { Sku = "sku-1", Quantity = 2 }, cancellationToken);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        var received = handler.ReceivedMessages.ShouldHaveSingleItem().ShouldBeOfType<StockReserved>();
                        received.Sku.ShouldBe("sku-1");
                        received.Quantity.ShouldBe(2);
                    });
            });
    }

    [Test]
    public async Task Then_A_Batch_Of_Derived_Messages_Is_Published_To_The_Base_Type()
    {
        // Arrange
        var handler = new InspectableHandler<WarehouseEvent>();

        var services = GivenSystemTextJson()
            .ConfigureJustSaying(builder => builder
                .Publications(pub => pub.WithTopic<WarehouseEvent>())
                .Subscriptions(sub => sub.ForTopic<WarehouseEvent>(c => c.WithQueueName(UniqueName))))
            .AddSingleton<IHandlerAsync<WarehouseEvent>>(handler);

        await WhenBatchAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                List<WarehouseEvent> batch =
                [
                    new StockReserved { Sku = "sku-1", Quantity = 2 },
                    new StockReleased { Sku = "sku-2", Reason = "cancelled" },
                ];
                await publisher.PublishBatchAsync(batch, cancellationToken);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.ReceivedMessages.Count.ShouldBe(2);
                        handler.ReceivedMessages.OfType<StockReserved>().ShouldHaveSingleItem().Quantity.ShouldBe(2);
                        handler.ReceivedMessages.OfType<StockReleased>().ShouldHaveSingleItem().Reason.ShouldBe("cancelled");
                    });
            });
    }

    [Test]
    public async Task Then_A_Dynamic_Base_Type_Publication_Receives_The_Derived_Message()
    {
        // Arrange
        var handler = new InspectableHandler<WarehouseEvent>();
        var topicName = $"{UniqueName}-sku-1";

        var services = GivenSystemTextJson()
            .ConfigureJustSaying(builder => builder
                .Publications(pub => pub.WithTopic<WarehouseEvent>(c => c.WithTopicName(msg => $"{UniqueName}-{msg.Sku}")))
                .Subscriptions(sub => sub.ForTopic<WarehouseEvent>(c => c.WithTopicName(topicName).WithQueueName(UniqueName))))
            .AddSingleton<IHandlerAsync<WarehouseEvent>>(handler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new StockReserved { Sku = "sku-1", Quantity = 2 }, cancellationToken);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () => handler.ReceivedMessages.ShouldHaveSingleItem().ShouldBeOfType<StockReserved>().Quantity.ShouldBe(2));
            });
    }

    [Test]
    public async Task Then_A_Publication_For_The_Exact_Type_Wins()
    {
        // Arrange
        var baseHandler = new InspectableHandler<WarehouseEvent>();
        var releasedHandler = new InspectableHandler<StockReleased>();

        var services = GivenSystemTextJson()
            .ConfigureJustSaying(builder => builder
                .Publications(pub =>
                {
                    pub.WithQueue<WarehouseEvent>(c => c.WithQueueName(UniqueName + "-base"));
                    pub.WithQueue<StockReleased>(c => c.WithQueueName(UniqueName + "-released"));
                })
                .Subscriptions(sub =>
                {
                    sub.ForQueue<WarehouseEvent>(c => c.WithQueueName(UniqueName + "-base"));
                    sub.ForQueue<StockReleased>(c => c.WithQueueName(UniqueName + "-released"));
                }))
            .AddSingleton<IHandlerAsync<WarehouseEvent>>(baseHandler)
            .AddSingleton<IHandlerAsync<StockReleased>>(releasedHandler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new StockReleased { Sku = "sku-1", Reason = "cancelled" }, cancellationToken);
                await publisher.PublishAsync(new StockReserved { Sku = "sku-2", Quantity = 2 }, cancellationToken);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        releasedHandler.ReceivedMessages.ShouldHaveSingleItem().Reason.ShouldBe("cancelled");
                        baseHandler.ReceivedMessages.ShouldHaveSingleItem().ShouldBeOfType<StockReserved>().Sku.ShouldBe("sku-2");
                    });
            });
    }

    public abstract class PlainWarehouseEvent
    {
        public string Sku { get; set; }
    }

    [Test]
    public void Then_A_Base_Type_Not_Configured_For_Polymorphism_Fails_At_Bus_Build()
    {
        // Arrange
        var serviceProvider = GivenSystemTextJson()
            .ConfigureJustSaying(builder => builder.Publications(pub => pub.WithTopic<PlainWarehouseEvent>()))
            .BuildServiceProvider();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagePublisher>());
        exception.Message.ShouldContain(typeof(PlainWarehouseEvent).ToString());
        exception.Message.ShouldContain("[JsonPolymorphic]");
    }

    private IServiceCollection GivenSystemTextJson()
        => GivenJustSaying()
            .AddSingleton<IMessageBodySerializationFactory>(
                new SystemTextJsonSerializationFactory(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions));
}
