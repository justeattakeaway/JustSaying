using JustSaying.Extensions.Kafka.Partitioning;
using JustSaying.Extensions.Kafka.Streams;
using JustSaying.Models;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Streams;

public class KafkaStreamBuilderTests
{
    #region Builder Configuration Tests

    [Fact]
    public void Constructor_SetsSourceTopic()
    {
        // Arrange & Act
        var builder = new KafkaStreamBuilder<TestMessage>("input-topic");

        // Assert
        builder.GetSourceTopic().ShouldBe("input-topic");
    }

    [Fact]
    public void Constructor_ThrowsForNullTopic()
    {
        Should.Throw<ArgumentNullException>(() => new KafkaStreamBuilder<TestMessage>(null));
    }

    [Fact]
    public void WithBootstrapServers_SetsConfiguration()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic");

        // Act
        builder.WithBootstrapServers("localhost:9092");
        var config = builder.GetConfiguration();

        // Assert
        config.BootstrapServers.ShouldBe("localhost:9092");
    }

    [Fact]
    public void WithGroupId_SetsConfiguration()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic");

        // Act
        builder.WithGroupId("my-stream-group");
        var config = builder.GetConfiguration();

        // Assert
        config.GroupId.ShouldBe("my-stream-group");
    }

    [Fact]
    public void WithCloudEvents_SetsConfiguration()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic");

        // Act
        builder.WithCloudEvents(true, "urn:myapp:streams");
        var config = builder.GetConfiguration();

        // Assert
        config.EnableCloudEvents.ShouldBeTrue();
        config.CloudEventsSource.ShouldBe("urn:myapp:streams");
    }

    [Fact]
    public void To_SetsSinkTopic()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("input");

        // Act
        builder.To("output");

        // Assert
        builder.GetSinkTopic().ShouldBe("output");
    }

    [Fact]
    public void WithSinkPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic")
            .WithBootstrapServers("localhost:9092");
        var strategy = RoundRobinPartitionKeyStrategy.Instance;

        // Act
        builder.WithSinkPartitioning(strategy);

        // Assert - verify it doesn't throw
        Should.NotThrow(() => builder.Build());
    }

    #endregion

    #region Filter Operation Tests

    [Fact]
    public void Filter_AddsFilterOperation()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic")
            .WithBootstrapServers("localhost:9092");

        // Act
        builder.Filter(m => m.Data != null);
        var handler = builder.Build();

        // Assert
        handler.Operations.Count.ShouldBe(1);
        handler.Operations[0].OperationType.ShouldBe("Filter");
    }

    [Fact]
    public void Filter_WithContext_AddsFilterOperation()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic")
            .WithBootstrapServers("localhost:9092");

        // Act
        builder.Filter((m, ctx) => ctx.Topic == "topic");
        var handler = builder.Build();

        // Assert
        handler.Operations.Count.ShouldBe(1);
    }

    [Fact]
    public void Filter_CanChainMultiple()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic")
            .WithBootstrapServers("localhost:9092");

        // Act
        builder
            .Filter(m => m.Data != null)
            .Filter(m => m.Data.Length > 0);
        var handler = builder.Build();

        // Assert
        handler.Operations.Count.ShouldBe(2);
    }

    #endregion

    #region Map Operation Tests

    [Fact]
    public void Map_TransformsToNewType()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("input");

        // Act
        var newBuilder = builder.Map(m => new OutputMessage { Result = m.Data });

        // Assert
        newBuilder.GetSourceTopic().ShouldBe("input");
    }

    [Fact]
    public void Map_WithContext_Works()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("input")
            .WithBootstrapServers("localhost:9092");

        // Act
        var newBuilder = builder.Map((m, ctx) => new OutputMessage 
        { 
            Result = $"{ctx.Topic}:{m.Data}" 
        });

        // Assert
        var handler = newBuilder.Build();
        handler.Operations.Count.ShouldBe(1);
    }

    [Fact]
    public void Map_PreservesConfiguration()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("input")
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("test-group")
            .WithCloudEvents(true);

        // Act
        var newBuilder = builder.Map(m => new OutputMessage());

        // Assert
        var config = newBuilder.GetConfiguration();
        config.BootstrapServers.ShouldBe("localhost:9092");
        config.GroupId.ShouldBe("test-group");
        config.EnableCloudEvents.ShouldBeTrue();
    }

    #endregion

    #region FlatMap Operation Tests

    [Fact]
    public void FlatMap_Works()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("input")
            .WithBootstrapServers("localhost:9092");

        // Act
        var newBuilder = builder.FlatMap(m => 
            m.Data?.Split(',').Select(s => new OutputMessage { Result = s }) 
            ?? Enumerable.Empty<OutputMessage>());

        // Assert
        var handler = newBuilder.Build();
        handler.Operations.Count.ShouldBe(1);
    }

    #endregion

    #region Peek Operation Tests

    [Fact]
    public void Peek_AddsSideEffect()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic")
            .WithBootstrapServers("localhost:9092");
        var peekedMessages = new List<TestMessage>();

        // Act
        builder.Peek(m => peekedMessages.Add(m));
        var handler = builder.Build();

        // Assert
        handler.Operations.Count.ShouldBe(1);
        handler.Operations[0].OperationType.ShouldBe("Peek");
    }

    [Fact]
    public void PeekAsync_AddsAsyncSideEffect()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("topic")
            .WithBootstrapServers("localhost:9092");

        // Act
        builder.PeekAsync(async (m, ctx, ct) => 
        {
            await Task.Delay(1, ct);
        });
        var handler = builder.Build();

        // Assert
        handler.Operations.Count.ShouldBe(1);
        handler.Operations[0].OperationType.ShouldBe("PeekAsync");
    }

    #endregion

    #region Branch Operation Tests

    [Fact]
    public void Branch_AddsBranchOperation()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("input")
            .WithBootstrapServers("localhost:9092");

        // Act
        builder.Branch(
            (m => m.Data == "A", "topic-a"),
            (m => m.Data == "B", "topic-b")
        );
        var handler = builder.Build();

        // Assert
        handler.Operations.Count.ShouldBe(1);
        handler.Operations[0].OperationType.ShouldBe("Branch");
    }

    #endregion

    #region GroupBy and Windowing Tests

    [Fact]
    public void GroupBy_ReturnsGroupedBuilder()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<OrderMessage>("orders");

        // Act
        var groupedBuilder = builder.GroupBy(m => m.CustomerId);

        // Assert
        groupedBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void GroupBy_WindowedBy_ReturnsWindowedBuilder()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<OrderMessage>("orders");

        // Act
        var windowedBuilder = builder
            .GroupBy(m => m.CustomerId)
            .WindowedBy(TimeSpan.FromMinutes(5));

        // Assert
        windowedBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void GroupBy_SlidingWindowedBy_Works()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<OrderMessage>("orders");

        // Act
        var windowedBuilder = builder
            .GroupBy(m => m.CustomerId)
            .SlidingWindowedBy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));

        // Assert
        windowedBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void GroupBy_SessionWindowedBy_Works()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<OrderMessage>("orders");

        // Act
        var windowedBuilder = builder
            .GroupBy(m => m.CustomerId)
            .SessionWindowedBy(TimeSpan.FromMinutes(30));

        // Assert
        windowedBuilder.ShouldNotBeNull();
    }

    #endregion

    #region StreamHandler Tests

    [Fact]
    public void Build_ReturnsStreamHandler()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("input")
            .WithBootstrapServers("localhost:9092")
            .Filter(m => m.Data != null)
            .To("output");

        // Act
        var handler = builder.Build();

        // Assert
        handler.ShouldNotBeNull();
        handler.SourceTopic.ShouldBe("input");
        handler.SinkTopic.ShouldBe("output");
    }

    [Fact]
    public void Build_ThrowsForInvalidConfiguration()
    {
        // Arrange
        var builder = new KafkaStreamBuilder<TestMessage>("input");
        // No bootstrap servers configured

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void ComplexPipeline_CanBeBuilt()
    {
        // Arrange & Act
        var handler = new KafkaStreamBuilder<OrderMessage>("orders")
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("order-processor")
            .Filter(o => o.Status != "Cancelled")
            .Filter(o => o.Amount > 0)
            .Peek(o => Console.WriteLine($"Processing: {o.OrderId}"))
            .Map(o => new ShippingMessage 
            { 
                OrderId = o.OrderId, 
                Address = o.ShippingAddress 
            })
            .To("shipping-events")
            .Build();

        // Assert
        handler.ShouldNotBeNull();
        handler.SourceTopic.ShouldBe("orders");
        handler.SinkTopic.ShouldBe("shipping-events");
        handler.Operations.Count.ShouldBe(4); // 2 filters + peek + map
    }

    #endregion

    #region Test Messages

    public class TestMessage : Message
    {
        public string Data { get; set; }
    }

    public class OutputMessage : Message
    {
        public string Result { get; set; }
    }

    public class OrderMessage : Message
    {
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string ShippingAddress { get; set; }
    }

    public class ShippingMessage : Message
    {
        public string OrderId { get; set; }
        public string Address { get; set; }
    }

    #endregion
}
