using JustSaying.Extensions.Kafka.Streams;
using JustSaying.Models;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Streams;

public class StreamProcessorTests
{
    #region MapProcessor Tests

    [Fact]
    public async Task MapProcessor_TransformsMessage()
    {
        // Arrange
        var processor = new MapProcessor<OrderMessage, ShippingMessage>(
            (order, _) => new ShippingMessage 
            { 
                OrderId = order.OrderId, 
                Address = order.ShippingAddress 
            });
        
        var input = new OrderMessage { OrderId = "123", ShippingAddress = "123 Main St" };
        var context = new StreamContext { Topic = "orders" };

        // Act
        var results = await processor.ProcessAsync(input, context, TestContext.Current.CancellationToken);

        // Assert
        var resultList = results.ToList();
        resultList.Count.ShouldBe(1);
        resultList[0].OrderId.ShouldBe("123");
        resultList[0].Address.ShouldBe("123 Main St");
    }

    [Fact]
    public async Task MapProcessor_ReturnsEmptyForNullResult()
    {
        // Arrange
        var processor = new MapProcessor<OrderMessage, ShippingMessage>(
            (order, _) => null);
        
        var input = new OrderMessage { OrderId = "123" };
        var context = new StreamContext { Topic = "orders" };

        // Act
        var results = await processor.ProcessAsync(input, context, TestContext.Current.CancellationToken);

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void MapProcessor_ThrowsForNullMapper()
    {
        Should.Throw<ArgumentNullException>(() => 
            new MapProcessor<OrderMessage, ShippingMessage>(null));
    }

    #endregion

    #region FilterProcessor Tests

    [Fact]
    public async Task FilterProcessor_PassesMatchingMessages()
    {
        // Arrange
        var processor = new FilterProcessor<OrderMessage>(
            (order, _) => order.Amount > 100);
        
        var input = new OrderMessage { OrderId = "123", Amount = 150 };
        var context = new StreamContext { Topic = "orders" };

        // Act
        var results = await processor.ProcessAsync(input, context, TestContext.Current.CancellationToken);

        // Assert
        var resultList = results.ToList();
        resultList.Count.ShouldBe(1);
        resultList[0].ShouldBe(input);
    }

    [Fact]
    public async Task FilterProcessor_FiltersNonMatchingMessages()
    {
        // Arrange
        var processor = new FilterProcessor<OrderMessage>(
            (order, _) => order.Amount > 100);
        
        var input = new OrderMessage { OrderId = "123", Amount = 50 };
        var context = new StreamContext { Topic = "orders" };

        // Act
        var results = await processor.ProcessAsync(input, context, TestContext.Current.CancellationToken);

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void FilterProcessor_ThrowsForNullPredicate()
    {
        Should.Throw<ArgumentNullException>(() => 
            new FilterProcessor<OrderMessage>((Func<OrderMessage, StreamContext, bool>)null));
    }

    #endregion

    #region FlatMapProcessor Tests

    [Fact]
    public async Task FlatMapProcessor_ProducesMultipleMessages()
    {
        // Arrange
        var processor = new FlatMapProcessor<OrderMessage, LineItemMessage>(
            (order, _) => order.Items.Select(i => new LineItemMessage 
            { 
                OrderId = order.OrderId, 
                ProductId = i 
            }));
        
        var input = new OrderMessage 
        { 
            OrderId = "123", 
            Items = new List<string> { "A", "B", "C" } 
        };
        var context = new StreamContext { Topic = "orders" };

        // Act
        var results = await processor.ProcessAsync(input, context, TestContext.Current.CancellationToken);

        // Assert
        var resultList = results.ToList();
        resultList.Count.ShouldBe(3);
        resultList[0].ProductId.ShouldBe("A");
        resultList[1].ProductId.ShouldBe("B");
        resultList[2].ProductId.ShouldBe("C");
    }

    [Fact]
    public async Task FlatMapProcessor_HandlesEmptyOutput()
    {
        // Arrange
        var processor = new FlatMapProcessor<OrderMessage, LineItemMessage>(
            (order, _) => Enumerable.Empty<LineItemMessage>());
        
        var input = new OrderMessage { OrderId = "123" };
        var context = new StreamContext { Topic = "orders" };

        // Act
        var results = await processor.ProcessAsync(input, context, TestContext.Current.CancellationToken);

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task FlatMapProcessor_HandlesNullOutput()
    {
        // Arrange
        var processor = new FlatMapProcessor<OrderMessage, LineItemMessage>(
            (order, _) => null);
        
        var input = new OrderMessage { OrderId = "123" };
        var context = new StreamContext { Topic = "orders" };

        // Act
        var results = await processor.ProcessAsync(input, context, TestContext.Current.CancellationToken);

        // Assert
        results.ShouldBeEmpty();
    }

    #endregion

    #region AsyncProcessor Tests

    [Fact]
    public async Task AsyncProcessor_ProcessesAsynchronously()
    {
        // Arrange
        var processor = new AsyncProcessor<OrderMessage, ShippingMessage>(
            async (order, ctx, ct) =>
            {
                await Task.Delay(1, ct);
                return new[] 
                { 
                    new ShippingMessage { OrderId = order.OrderId } 
                };
            });
        
        var input = new OrderMessage { OrderId = "async-123" };
        var context = new StreamContext { Topic = "orders" };

        // Act
        var results = await processor.ProcessAsync(input, context, TestContext.Current.CancellationToken);

        // Assert
        var resultList = results.ToList();
        resultList.Count.ShouldBe(1);
        resultList[0].OrderId.ShouldBe("async-123");
    }

    [Fact]
    public async Task AsyncProcessor_SupportsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var processor = new AsyncProcessor<OrderMessage, ShippingMessage>(
            async (order, ctx, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(1000, ct);
                return Array.Empty<ShippingMessage>();
            });
        
        var input = new OrderMessage { OrderId = "123" };
        var context = new StreamContext { Topic = "orders" };

        // Act
        cts.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => processor.ProcessAsync(input, context, cts.Token));
    }

    #endregion

    #region StreamContext Tests

    [Fact]
    public void StreamContext_PropertiesCanBeSet()
    {
        // Arrange & Act
        var context = new StreamContext
        {
            Topic = "test-topic",
            Partition = 5,
            Offset = 12345,
            Key = "msg-key",
            Timestamp = DateTime.UtcNow,
            OutputTopic = "output-topic",
            Headers = new Dictionary<string, string> { ["h1"] = "v1" }
        };

        // Assert
        context.Topic.ShouldBe("test-topic");
        context.Partition.ShouldBe(5);
        context.Offset.ShouldBe(12345);
        context.Key.ShouldBe("msg-key");
        context.OutputTopic.ShouldBe("output-topic");
        context.Headers["h1"].ShouldBe("v1");
    }

    [Fact]
    public void StreamContext_StateIsInitialized()
    {
        // Arrange & Act
        var context = new StreamContext();

        // Assert
        context.State.ShouldNotBeNull();
        context.State.ShouldBeEmpty();
    }

    [Fact]
    public void StreamContext_StateCanStoreValues()
    {
        // Arrange
        var context = new StreamContext();

        // Act
        context.State["counter"] = 42;
        context.State["name"] = "test";

        // Assert
        context.State["counter"].ShouldBe(42);
        context.State["name"].ShouldBe("test");
    }

    #endregion

    #region Test Messages

    public class OrderMessage : Message
    {
        public string OrderId { get; set; }
        public string ShippingAddress { get; set; }
        public decimal Amount { get; set; }
        public List<string> Items { get; set; } = new();
    }

    public class ShippingMessage : Message
    {
        public string OrderId { get; set; }
        public string Address { get; set; }
    }

    public class LineItemMessage : Message
    {
        public string OrderId { get; set; }
        public string ProductId { get; set; }
    }

    #endregion
}
