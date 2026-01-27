using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace JustSaying.Extensions.Kafka.Tests.Messaging;

public class WorkerTestMessage : Message
{
    public string Content { get; set; }
}

public class WorkerTestMessageHandler : IHandlerAsync<WorkerTestMessage>
{
    public Task<bool> Handle(WorkerTestMessage message)
    {
        return Task.FromResult(true);
    }
}

public class KafkaMessagingExtensionsWorkerTests
{
    [Fact]
    public void AddKafkaConsumerWorker_NullTopic_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddKafkaConsumerWorker<WorkerTestMessage>(
                null,
                config => config.BootstrapServers = "localhost:9092"));
    }

    [Fact]
    public void AddKafkaConsumerWorker_EmptyTopic_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddKafkaConsumerWorker<WorkerTestMessage>(
                string.Empty,
                config => config.BootstrapServers = "localhost:9092"));
    }

    [Fact]
    public void AddKafkaConsumerWorker_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddKafkaConsumerWorker<WorkerTestMessage>("test-topic", null));
    }

    [Fact]
    public void AddKafkaConsumerWorker_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBodySerializationFactory>(
            new SystemTextJsonSerializationFactory(new JsonSerializerOptions()));
        services.AddSingleton<IHandlerAsync<WorkerTestMessage>, WorkerTestMessageHandler>();

        // Act
        services.AddKafkaConsumerWorker<WorkerTestMessage>("test-topic", config =>
        {
            config.BootstrapServers = "localhost:9092";
            config.GroupId = "test-group";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, hs => hs.GetType().Name.Contains("KafkaConsumerWorker"));
    }

    [Fact]
    public void AddKafkaConsumerWorker_MultipleConsumers_RegistersMultipleHostedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBodySerializationFactory>(
            new SystemTextJsonSerializationFactory(new JsonSerializerOptions()));
        services.AddSingleton<IHandlerAsync<WorkerTestMessage>, WorkerTestMessageHandler>();

        // Act
        services.AddKafkaConsumerWorker<WorkerTestMessage>("test-topic", config =>
        {
            config.BootstrapServers = "localhost:9092";
            config.GroupId = "test-group";
            config.NumberOfConsumers = 3;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        var workerCount = hostedServices.Count(hs => hs.GetType().Name.Contains("KafkaConsumerWorker"));
        Assert.Equal(3, workerCount);
    }

    [Fact]
    public void AddKafkaConsumerWorker_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddKafkaConsumerWorker<WorkerTestMessage>("test-topic", config =>
        {
            config.BootstrapServers = "localhost:9092";
            config.GroupId = "test-group";
        });

        // Assert
        Assert.Same(services, result);
    }
}

