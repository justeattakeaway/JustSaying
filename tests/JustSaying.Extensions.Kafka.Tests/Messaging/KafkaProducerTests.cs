using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Factory;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text.Json;

namespace JustSaying.Extensions.Kafka.Tests.Messaging;

/// <summary>
/// Marker type for testing typed producer.
/// </summary>
public class TestProducerType { }

public class TestOrderMessage : Message
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}

public class KafkaProducerTests
{
    private readonly KafkaConfiguration _configuration;
    private readonly IKafkaProducerFactory _producerFactory;
    private readonly IMessageBodySerializationFactory _serializationFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IProducer<string, byte[]> _mockProducer;

    public KafkaProducerTests()
    {
        _configuration = new KafkaConfiguration
        {
            BootstrapServers = "localhost:9092",
            CloudEventsSource = "//test/source"
        };

        _mockProducer = Substitute.For<IProducer<string, byte[]>>();
        _producerFactory = Substitute.For<IKafkaProducerFactory>();
        _producerFactory.CreateProducer(Arg.Any<KafkaConfiguration>()).Returns(_mockProducer);

        _serializationFactory = new SystemTextJsonSerializationFactory(new JsonSerializerOptions());
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaProducer<TestProducerType>(
                null,
                _producerFactory,
                _serializationFactory,
                _loggerFactory));
    }

    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaProducer<TestProducerType>(
                _configuration,
                null,
                _serializationFactory,
                _loggerFactory));
    }

    [Fact]
    public void Constructor_NullSerializationFactory_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaProducer<TestProducerType>(
                _configuration,
                _producerFactory,
                null,
                _loggerFactory));
    }

    [Fact]
    public void Constructor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaProducer<TestProducerType>(
                _configuration,
                _producerFactory,
                _serializationFactory,
                null));
    }

    [Fact]
    public async Task ProduceAsync_NullTopic_ThrowsArgumentException()
    {
        // Arrange
        using var producer = CreateProducer();
        var message = new TestOrderMessage { OrderId = "ORD-123", Amount = 99.99m };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            producer.ProduceAsync(null, message, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProduceAsync_EmptyTopic_ThrowsArgumentException()
    {
        // Arrange
        using var producer = CreateProducer();
        var message = new TestOrderMessage { OrderId = "ORD-123", Amount = 99.99m };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            producer.ProduceAsync(string.Empty, message, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProduceAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        using var producer = CreateProducer();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            producer.ProduceAsync<TestOrderMessage>("test-topic", null, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProduceAsync_ValidMessage_CallsProducerAndReturnsTrue()
    {
        // Arrange
        using var producer = CreateProducer();
        var message = new TestOrderMessage { OrderId = "ORD-123", Amount = 99.99m };

        var deliveryResult = new DeliveryResult<string, byte[]>
        {
            Topic = "test-topic",
            Partition = new Partition(0),
            Offset = new Offset(42)
        };

        _mockProducer.ProduceAsync(
            Arg.Any<string>(),
            Arg.Any<Message<string, byte[]>>(),
            Arg.Any<CancellationToken>())
            .Returns(deliveryResult);

        // Act
        var result = await producer.ProduceAsync("test-topic", message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        await _mockProducer.Received(1).ProduceAsync(
            "test-topic",
            Arg.Any<Message<string, byte[]>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProduceAsync_CustomKey_UsesProvidedKey()
    {
        // Arrange
        using var producer = CreateProducer();
        var message = new TestOrderMessage { OrderId = "ORD-123", Amount = 99.99m };
        var customKey = "custom-partition-key";

        var deliveryResult = new DeliveryResult<string, byte[]>
        {
            Topic = "test-topic",
            Partition = new Partition(0),
            Offset = new Offset(42)
        };

        _mockProducer.ProduceAsync(
            Arg.Any<string>(),
            Arg.Any<Message<string, byte[]>>(),
            Arg.Any<CancellationToken>())
            .Returns(deliveryResult);

        // Act
        await producer.ProduceAsync("test-topic", message, key: customKey, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await _mockProducer.Received(1).ProduceAsync(
            "test-topic",
            Arg.Is<Message<string, byte[]>>(m => m.Key == customKey),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProduceAsync_ProduceException_ReturnsFalse()
    {
        // Arrange
        using var producer = CreateProducer();
        var message = new TestOrderMessage { OrderId = "ORD-123", Amount = 99.99m };

        _mockProducer.ProduceAsync(
            Arg.Any<string>(),
            Arg.Any<Message<string, byte[]>>(),
            Arg.Any<CancellationToken>())
            .Throws(new ProduceException<string, byte[]>(
                new Error(ErrorCode.BrokerNotAvailable, "Broker unavailable"),
                new DeliveryResult<string, byte[]>()));

        // Act
        var result = await producer.ProduceAsync("test-topic", message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Produce_NullTopic_ThrowsArgumentException()
    {
        // Arrange
        using var producer = CreateProducer();
        var message = new TestOrderMessage { OrderId = "ORD-123", Amount = 99.99m };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            producer.Produce(null, message, _ => { }));
    }

    [Fact]
    public void Produce_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        using var producer = CreateProducer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            producer.Produce<TestOrderMessage>("test-topic", null, _ => { }));
    }

    [Fact]
    public void Produce_NullDeliveryHandler_ThrowsArgumentNullException()
    {
        // Arrange
        using var producer = CreateProducer();
        var message = new TestOrderMessage { OrderId = "ORD-123", Amount = 99.99m };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            producer.Produce("test-topic", message, null));
    }

    [Fact]
    public void Produce_ValidMessage_CallsProducerWithCallback()
    {
        // Arrange
        using var producer = CreateProducer();
        var message = new TestOrderMessage { OrderId = "ORD-123", Amount = 99.99m };
        Action<DeliveryReport<string, byte[]>> callback = _ => { };

        // Act
        producer.Produce("test-topic", message, callback);

        // Assert
        _mockProducer.Received(1).Produce(
            "test-topic",
            Arg.Any<Message<string, byte[]>>(),
            Arg.Any<Action<DeliveryReport<string, byte[]>>>());
    }

    [Fact]
    public void Flush_CallsProducerFlush()
    {
        // Arrange
        using var producer = CreateProducer();
        var timeout = TimeSpan.FromSeconds(5);

        _mockProducer.Flush(Arg.Any<TimeSpan>()).Returns(0);

        // Act
        var result = producer.Flush(timeout);

        // Assert
        Assert.Equal(0, result);
        _mockProducer.Received(1).Flush(timeout);
    }

    [Fact]
    public async Task Dispose_CannotProduceAfterDispose()
    {
        // Arrange
        var producer = CreateProducer();
        producer.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            producer.ProduceAsync("test-topic", new TestOrderMessage(), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Dispose_CannotFlushAfterDispose()
    {
        // Arrange
        var producer = CreateProducer();
        producer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            producer.Flush(TimeSpan.FromSeconds(5)));
    }

    private KafkaProducer<TestProducerType> CreateProducer()
    {
        return new KafkaProducer<TestProducerType>(
            _configuration,
            _producerFactory,
            _serializationFactory,
            _loggerFactory);
    }
}
