using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Factory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Factory;

public class KafkaFactoryTests
{
    #region IKafkaConsumerFactory Tests

    [Fact]
    public void KafkaConsumerFactory_Constructor_RequiresLoggerFactory()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new KafkaConsumerFactory(null));
    }

    [Fact]
    public void KafkaConsumerFactory_CreateConsumer_RequiresConfiguration()
    {
        // Arrange
        var factory = new KafkaConsumerFactory(NullLoggerFactory.Instance);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => factory.CreateConsumer(null, "test-id"));
    }

    [Fact]
    public void KafkaConsumerFactory_CreateConsumer_CreatesConsumerWithValidConfig()
    {
        // Arrange
        var factory = new KafkaConsumerFactory(NullLoggerFactory.Instance);
        var config = new KafkaConfiguration
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group"
        };

        // Act - The factory should create a consumer successfully
        // The consumer won't be connected since Kafka is not available, but the object is created
        var consumer = factory.CreateConsumer(config, "test-consumer");

        // Assert
        consumer.ShouldNotBeNull();

        // Clean up
        consumer.Dispose();
    }

    #endregion

    #region IKafkaProducerFactory Tests

    [Fact]
    public void KafkaProducerFactory_Constructor_RequiresLoggerFactory()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new KafkaProducerFactory(null));
    }

    [Fact]
    public void KafkaProducerFactory_CreateProducer_RequiresConfiguration()
    {
        // Arrange
        var factory = new KafkaProducerFactory(NullLoggerFactory.Instance);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => factory.CreateProducer(null));
    }

    [Fact]
    public void KafkaProducerFactory_CreateProducer_CreatesProducerWithValidConfig()
    {
        // Arrange
        var factory = new KafkaProducerFactory(NullLoggerFactory.Instance);
        var config = new KafkaConfiguration
        {
            BootstrapServers = "localhost:9092"
        };

        // Act - The factory should create a producer successfully
        var producer = factory.CreateProducer(config);

        // Assert
        producer.ShouldNotBeNull();

        // Clean up
        producer.Dispose();
    }

    #endregion

    #region DI Registration Tests

    [Fact]
    public void AddKafkaFactories_RegistersDefaultFactories()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafkaFactories();
        var provider = services.BuildServiceProvider();

        // Assert
        var consumerFactory = provider.GetService<IKafkaConsumerFactory>();
        var producerFactory = provider.GetService<IKafkaProducerFactory>();

        consumerFactory.ShouldNotBeNull();
        consumerFactory.ShouldBeOfType<KafkaConsumerFactory>();

        producerFactory.ShouldNotBeNull();
        producerFactory.ShouldBeOfType<KafkaProducerFactory>();
    }

    [Fact]
    public void AddKafkaConsumerFactory_RegistersCustomFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafkaConsumerFactory<TestConsumerFactory>();
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<IKafkaConsumerFactory>();
        factory.ShouldNotBeNull();
        factory.ShouldBeOfType<TestConsumerFactory>();
    }

    [Fact]
    public void AddKafkaProducerFactory_RegistersCustomFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafkaProducerFactory<TestProducerFactory>();
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<IKafkaProducerFactory>();
        factory.ShouldNotBeNull();
        factory.ShouldBeOfType<TestProducerFactory>();
    }

    [Fact]
    public void AddKafkaFactories_TryAdd_DoesNotOverrideExisting()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom factories first
        services.AddKafkaConsumerFactory<TestConsumerFactory>();
        services.AddKafkaProducerFactory<TestProducerFactory>();

        // Act - This should not override the existing registrations
        services.AddKafkaFactories();
        var provider = services.BuildServiceProvider();

        // Assert - Custom factories should still be registered
        var consumerFactory = provider.GetService<IKafkaConsumerFactory>();
        consumerFactory.ShouldBeOfType<TestConsumerFactory>();
    }

    #endregion

    #region Test Implementations

    public class TestConsumerFactory : IKafkaConsumerFactory
    {
        public Confluent.Kafka.IConsumer<string, byte[]> CreateConsumer(
            KafkaConfiguration configuration,
            string consumerId)
        {
            return Substitute.For<Confluent.Kafka.IConsumer<string, byte[]>>();
        }
    }

    public class TestProducerFactory : IKafkaProducerFactory
    {
        public Confluent.Kafka.IProducer<string, byte[]> CreateProducer(KafkaConfiguration configuration)
        {
            return Substitute.For<Confluent.Kafka.IProducer<string, byte[]>>();
        }
    }

    #endregion
}

