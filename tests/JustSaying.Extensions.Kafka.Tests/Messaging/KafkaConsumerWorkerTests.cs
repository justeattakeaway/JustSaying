using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.Extensions.Kafka.Tests.Messaging;

public class TestWorkerMessage : Message
{
    public string Content { get; set; }
}

public class KafkaConsumerWorkerTests
{
    private readonly KafkaConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    public KafkaConsumerWorkerTests()
    {
        _configuration = new KafkaConfiguration
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group"
        };

        _serviceProvider = Substitute.For<IServiceProvider>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
    }

    [Fact]
    public void Constructor_NullConsumerId_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaConsumerWorker<TestWorkerMessage>(
                null,
                "test-topic",
                _configuration,
                _serviceProvider,
                _loggerFactory));
    }

    [Fact]
    public void Constructor_NullTopic_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaConsumerWorker<TestWorkerMessage>(
                "consumer-1",
                null,
                _configuration,
                _serviceProvider,
                _loggerFactory));
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaConsumerWorker<TestWorkerMessage>(
                "consumer-1",
                "test-topic",
                null,
                _serviceProvider,
                _loggerFactory));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaConsumerWorker<TestWorkerMessage>(
                "consumer-1",
                "test-topic",
                _configuration,
                null,
                _loggerFactory));
    }

    [Fact]
    public void Constructor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KafkaConsumerWorker<TestWorkerMessage>(
                "consumer-1",
                "test-topic",
                _configuration,
                _serviceProvider,
                null));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var worker = new KafkaConsumerWorker<TestWorkerMessage>(
            "consumer-1",
            "test-topic",
            _configuration,
            _serviceProvider,
            _loggerFactory);

        // Assert
        Assert.NotNull(worker);
    }
}

