using JustSaying.Extensions.Kafka.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.Extensions.Kafka.Tests.Messaging;

public class KafkaMessageContextAccessorTests
{
    [Fact]
    public void Context_InitiallyNull()
    {
        // Arrange
        var accessor = new KafkaMessageContextAccessor();

        // Assert
        Assert.Null(accessor.Context);
    }

    [Fact]
    public void Context_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var accessor = new KafkaMessageContextAccessor();
        var context = new KafkaMessageContext
        {
            Topic = "test-topic",
            Partition = 1
        };

        // Act
        accessor.Context = context;

        // Assert
        Assert.Same(context, accessor.Context);
    }

    [Fact]
    public void Context_SetToNull_ClearsContext()
    {
        // Arrange
        var accessor = new KafkaMessageContextAccessor();
        accessor.Context = new KafkaMessageContext { Topic = "test" };

        // Act
        accessor.Context = null;

        // Assert
        Assert.Null(accessor.Context);
    }

    [Fact]
    public async Task Context_FlowsAcrossAsyncBoundaries()
    {
        // Arrange
        var accessor = new KafkaMessageContextAccessor();
        var context = new KafkaMessageContext
        {
            Topic = "async-test",
            Partition = 5
        };

        // Act
        accessor.Context = context;
        await Task.Yield();

        // Assert - context should still be accessible after await
        Assert.NotNull(accessor.Context);
        Assert.Equal("async-test", accessor.Context.Topic);
        Assert.Equal(5, accessor.Context.Partition);
    }

    [Fact]
    public async Task Context_IsIsolatedBetweenAsyncFlows()
    {
        // Arrange
        var accessor = new KafkaMessageContextAccessor();
        var context1 = new KafkaMessageContext { Topic = "topic-1" };
        var context2 = new KafkaMessageContext { Topic = "topic-2" };

        string result1 = null;
        string result2 = null;
        var token = TestContext.Current.CancellationToken;

        // Act - run two parallel operations with different contexts
        var task1 = Task.Run(async () =>
        {
            accessor.Context = context1;
            await Task.Delay(50, token);
            result1 = accessor.Context?.Topic;
        }, token);

        var task2 = Task.Run(async () =>
        {
            accessor.Context = context2;
            await Task.Delay(30, token);
            result2 = accessor.Context?.Topic;
        }, token);

        await Task.WhenAll(task1, task2);

        // Assert - each flow should have its own context
        Assert.Equal("topic-1", result1);
        Assert.Equal("topic-2", result2);
    }

    [Fact]
    public void AddKafkaMessageContextAccessor_RegistersSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKafkaMessageContextAccessor();
        var provider = services.BuildServiceProvider();

        // Assert
        var accessor1 = provider.GetService<IKafkaMessageContextAccessor>();
        var accessor2 = provider.GetService<IKafkaMessageContextAccessor>();

        Assert.NotNull(accessor1);
        Assert.Same(accessor1, accessor2);
    }

    [Fact]
    public void AddKafkaMessageContextAccessor_DoesNotOverrideExisting()
    {
        // Arrange
        var services = new ServiceCollection();
        var customAccessor = new KafkaMessageContextAccessor();
        services.AddSingleton<IKafkaMessageContextAccessor>(customAccessor);

        // Act
        services.AddKafkaMessageContextAccessor();
        var provider = services.BuildServiceProvider();

        // Assert
        var resolved = provider.GetService<IKafkaMessageContextAccessor>();
        Assert.Same(customAccessor, resolved);
    }
}

