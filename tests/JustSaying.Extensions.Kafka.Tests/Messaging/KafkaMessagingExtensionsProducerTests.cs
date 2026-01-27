using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Factory;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JustSaying.Extensions.Kafka.Tests.Messaging;

/// <summary>
/// Marker types for testing typed producer registration.
/// </summary>
public class OrderServiceProducer { }
public class PaymentServiceProducer { }

public class KafkaMessagingExtensionsProducerTests
{
    [Fact]
    public void AddKafkaProducer_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddKafkaProducer<OrderServiceProducer>(null));
    }

    [Fact]
    public void AddKafkaProducer_RegistersTypedProducer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBodySerializationFactory>(
            new SystemTextJsonSerializationFactory(new JsonSerializerOptions()));

        // Act
        services.AddKafkaProducer<OrderServiceProducer>(config =>
        {
            config.BootstrapServers = "localhost:9092";
            config.CloudEventsSource = "//test/source";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var producer = provider.GetService<IKafkaProducer<OrderServiceProducer>>();
        Assert.NotNull(producer);
    }

    [Fact]
    public void AddKafkaProducer_MultipleProducers_RegistersDifferentInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBodySerializationFactory>(
            new SystemTextJsonSerializationFactory(new JsonSerializerOptions()));

        // Act
        services.AddKafkaProducer<OrderServiceProducer>(config =>
        {
            config.BootstrapServers = "localhost:9092";
            config.CloudEventsSource = "//orders/source";
        });

        services.AddKafkaProducer<PaymentServiceProducer>(config =>
        {
            config.BootstrapServers = "payments-cluster:9092";
            config.CloudEventsSource = "//payments/source";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var orderProducer = provider.GetService<IKafkaProducer<OrderServiceProducer>>();
        var paymentProducer = provider.GetService<IKafkaProducer<PaymentServiceProducer>>();

        Assert.NotNull(orderProducer);
        Assert.NotNull(paymentProducer);
        Assert.NotSame(orderProducer, paymentProducer);
    }

    [Fact]
    public void AddKafkaProducer_AlsoRegistersFactories()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBodySerializationFactory>(
            new SystemTextJsonSerializationFactory(new JsonSerializerOptions()));

        // Act
        services.AddKafkaProducer<OrderServiceProducer>(config =>
        {
            config.BootstrapServers = "localhost:9092";
            config.CloudEventsSource = "//test/source";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var consumerFactory = provider.GetService<IKafkaConsumerFactory>();
        var producerFactory = provider.GetService<IKafkaProducerFactory>();

        Assert.NotNull(consumerFactory);
        Assert.NotNull(producerFactory);
    }

    [Fact]
    public void AddKafkaProducer_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddKafkaProducer<OrderServiceProducer>(config =>
        {
            config.BootstrapServers = "localhost:9092";
        });

        // Assert
        Assert.Same(services, result);
    }
}
