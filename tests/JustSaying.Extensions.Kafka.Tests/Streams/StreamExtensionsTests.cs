using JustSaying.Extensions.Kafka.Streams;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Streams;

public class StreamExtensionsTests
{
    [Fact]
    public void Stream_CreatesBuilder()
    {
        // Act
        var builder = StreamExtensions.Stream<TestMessage>("my-topic");

        // Assert
        builder.ShouldNotBeNull();
        builder.GetSourceTopic().ShouldBe("my-topic");
    }

    [Fact]
    public void AddKafkaStream_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafkaStream<TestMessage>("orders", builder =>
        {
            builder
                .WithBootstrapServers("localhost:9092")
                .WithGroupId("test-group")
                .Filter(m => m.Data != null);
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IHostedService));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddKafkaStream_WithTopic_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafkaStream<TestMessage>("input-topic", builder =>
        {
            builder
                .WithBootstrapServers("localhost:9092")
                .WithGroupId("test-group");
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IHostedService));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddKafkaStream_ThrowsForNullConfigure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            services.AddKafkaStream<TestMessage>(null));
    }

    [Fact]
    public void AddKafkaStream_WithTopic_ThrowsForNullTopic()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() => 
            services.AddKafkaStream<TestMessage>(null, _ => { }));
    }

    [Fact]
    public void AddKafkaStream_WithTopic_ThrowsForEmptyTopic()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() => 
            services.AddKafkaStream<TestMessage>("", _ => { }));
    }

    [Fact]
    public void AddKafkaStream_WithTopic_ThrowsForNullConfigure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            services.AddKafkaStream<TestMessage>("topic", null));
    }

    public class TestMessage : Message
    {
        public string Data { get; set; }
    }
}
