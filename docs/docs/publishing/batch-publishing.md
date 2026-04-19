---
---

# Batch Publishing

Batch publishing allows you to publish multiple messages in a single AWS API call, reducing latency and improving throughput. Use `IMessageBatchPublisher` to publish batches of messages efficiently.

## When to Use Batch Publishing

Use batch publishing when:
- You need to publish multiple messages at once
- You want to reduce the number of AWS API calls
- You're processing bulk data or bulk operations
- Throughput is more important than individual message latency

## IMessageBatchPublisher Interface

Inject `IMessageBatchPublisher` instead of `IMessagePublisher` to publish message batches:

```csharp
public class OrderController : ControllerBase
{
    private readonly IMessageBatchPublisher _publisher;

    public OrderController(IMessageBatchPublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("bulk-orders")]
    public async Task<IActionResult> CreateBulkOrders(
        [FromBody] IReadOnlyCollection<CustomerOrderModel> orders)
    {
        var messages = orders.Select(order => new OrderPlacedEvent
        {
            OrderId = order.Id,
            Description = order.Description
        }).ToList();

        await _publisher.PublishAsync(messages);

        return Ok();
    }
}
```

## Configuration

Batch publishing uses the same configuration as regular publishing. Configure topics or queues using `Publications`:

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));

    config.Publications(x =>
    {
        // This topic supports both single and batch publishing
        x.WithTopic<OrderPlacedEvent>();
    });
});
```

Both `IMessagePublisher` and `IMessageBatchPublisher` are registered automatically when you call `AddJustSaying`.

## AWS Batch Limits

### SNS Batch Limits

SNS supports publishing up to **10 messages** per batch request. If you publish more than 10 messages, JustSaying automatically splits them into multiple batch requests.

```csharp
// Automatically split into 3 batch requests (10 + 10 + 5)
var messages = Enumerable.Range(1, 25)
    .Select(i => new OrderPlacedEvent { OrderId = i })
    .ToList();

await publisher.PublishAsync(messages);
```

### SQS Batch Limits

SQS also supports up to **10 messages** per batch request with automatic splitting for larger batches.

## Error Handling

Batch publishing can partially fail if some messages succeed and others fail. JustSaying handles this by:

1. Attempting to publish the entire batch
2. If any message fails, the entire batch operation throws an exception
3. Use retry configuration to handle transient failures

### Retry Configuration

Configure batch-specific retry behavior in messaging configuration:

```csharp
config.Messaging(x =>
{
    x.WithRegion("us-east-1");

    // Batch-specific retry settings
    x.WithPublishFailureBackoffForBatch(TimeSpan.FromSeconds(2));
    x.WithPublishFailureReattemptsForBatch(3);
});
```

See [Messaging Configuration](../messaging-configuration/README.md) for more retry options.

## Performance Considerations

### Benefits

- **Reduced API Calls**: Publish 10 messages in one request instead of 10 requests
- **Lower Latency**: Total time to publish multiple messages is reduced
- **Cost Savings**: Fewer AWS API calls mean lower costs
- **Higher Throughput**: Process bulk operations faster

### Trade-offs

- **All-or-Nothing**: If one message in a batch fails, the entire batch may fail
- **Larger Payload**: Batch requests have larger payloads that may hit size limits
- **Delayed Feedback**: Individual message publish status is not available immediately

## Complete Example

Here's a complete example from the Restaurant Ordering sample:

```csharp
// Controller endpoint
app.MapPost("api/multi-orders",
    async (IReadOnlyCollection<CustomerOrderModel> orders, IMessageBatchPublisher publisher) =>
    {
        app.Logger.LogInformation("Orders received: {@Orders}", orders);

        // Transform to messages
        var messages = orders.Select(order => new OrderPlacedEvent
        {
            OrderId = Random.Shared.Next(1, 100),
            Description = order.Description
        }).ToList();

        // Publish batch
        await publisher.PublishAsync(messages);

        app.Logger.LogInformation(
            "Orders {@OrderIds} placed",
            messages.Select(x => x.OrderId));

        return Results.Ok();
    });
```

## Single vs Batch Publishing

Both interfaces are available in your services:

```csharp
public class OrderService
{
    private readonly IMessagePublisher _publisher;
    private readonly IMessageBatchPublisher _batchPublisher;

    public OrderService(
        IMessagePublisher publisher,
        IMessageBatchPublisher batchPublisher)
    {
        _publisher = publisher;
        _batchPublisher = batchPublisher;
    }

    public async Task PublishSingleOrder(OrderPlacedEvent order)
    {
        // Single message
        await _publisher.PublishAsync(order);
    }

    public async Task PublishBulkOrders(IEnumerable<OrderPlacedEvent> orders)
    {
        // Multiple messages
        await _batchPublisher.PublishAsync(orders);
    }
}
```

## Best Practices

1. **Use batch publishing for bulk operations**: If you're processing multiple messages at once, always use `IMessageBatchPublisher`
2. **Don't batch unnecessarily**: For single messages, use `IMessagePublisher` instead
3. **Consider message size**: Ensure your batch doesn't exceed AWS payload limits (256KB for SNS)
4. **Handle partial failures**: Implement appropriate error handling for batch operations
5. **Monitor batch sizes**: Track how many messages you're batching to optimize performance

## See Also

- [Publications Configuration](configuration.md) - Configure topics and queues
- [Messaging Configuration](../messaging-configuration/README.md) - Batch retry configuration
- [Sample Application](../sample-application.md) - Complete batch publishing example
