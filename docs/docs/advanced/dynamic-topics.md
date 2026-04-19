---
---

# Dynamic Topics

Dynamic topics allow you to determine the topic name at runtime based on message content. This is useful for multi-tenant scenarios where each tenant needs isolated message streams.

## When to Use Dynamic Topics

Use dynamic topics when:
- Building multi-tenant applications with tenant isolation
- Routing messages based on content (e.g., region, environment)
- Need for logical separation of message streams
- Partitioning messages across multiple topics

## Configuration

Use `WithTopicName` with a function that takes the message and returns the topic name:

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));

    config.Publications(x =>
    {
        x.WithTopic<OrderPlacedEvent>(cfg =>
        {
            cfg.WithTopicName(msg =>
            {
                var order = (OrderPlacedEvent)msg;
                return $"tenant-{order.TenantId}-orders";
            });
        });
    });
});
```

## Multi-Tenant Example

### Message Definition

Define messages with tenant information:

```csharp
public class OrderPlacedEvent : Message
{
    public string TenantId { get; set; }
    public int OrderId { get; set; }
    public string Description { get; set; }
}
```

### Publisher Configuration

Configure dynamic topic naming based on tenant:

```csharp
config.Publications(x =>
{
    x.WithTopic<OrderPlacedEvent>(cfg =>
    {
        cfg.WithTopicName(msg =>
        {
            var order = (OrderPlacedEvent)msg;
            return $"tenant-{order.TenantId}-orders";
        });
    });
});
```

### Publishing Messages

Publish messages normally - the topic is determined automatically:

```csharp
// Publishes to topic: tenant-acme-orders
await publisher.PublishAsync(new OrderPlacedEvent
{
    TenantId = "acme",
    OrderId = 123,
    Description = "Office supplies"
});

// Publishes to topic: tenant-contoso-orders
await publisher.PublishAsync(new OrderPlacedEvent
{
    TenantId = "contoso",
    OrderId = 456,
    Description = "Electronics"
});
```

## Subscriber Configuration

Subscribers can use dynamic topic names as well:

```csharp
config.Subscriptions(x =>
{
    // Subscribe to specific tenant topics
    x.ForTopic<OrderPlacedEvent>("tenant-acme-orders");
    x.ForTopic<OrderPlacedEvent>("tenant-contoso-orders");
});

services.AddJustSayingHandler<OrderPlacedEvent, OrderPlacedEventHandler>();
```

For dynamic subscription scenarios, you may need to create separate bus instances per tenant.

## Performance Considerations

### Topic Creation Overhead

Each unique topic name requires AWS API calls to create the topic:

- First message to a new topic: ~1-2 seconds for topic creation
- Subsequent messages: Normal publish latency

**Recommendation**: Pre-create topics for known tenants using infrastructure-as-code (CloudFormation, Terraform).

### Topic Limits

AWS accounts have limits on SNS topics (default: 100,000 topics per region). Monitor topic count in multi-tenant scenarios:

```bash
aws sns list-topics --region us-east-1 | jq '.Topics | length'
```

### Caching

JustSaying caches topic ARNs after creation, so topic lookup overhead only occurs once per topic per application instance.

## Naming Conventions

### Best Practices

1. **Use consistent prefixes**: `tenant-{id}-{type}` makes topics easy to identify
2. **Lowercase names**: SNS topic names are case-insensitive but lowercase is convention
3. **Alphanumeric only**: Use letters, numbers, hyphens, and underscores
4. **Keep names short**: Topic names have a 256-character limit

### Examples

**Good**:
- `tenant-acme-orders`
- `region-us-east-payments`
- `env-prod-notifications`

**Avoid**:
- `Tenant_ACME_Orders` (mixed case, underscores)
- `tenant:acme:orders` (colons not recommended)
- Very long names with excessive detail

## Security and Isolation

### IAM Policies for Dynamic Topics

Grant permissions using wildcard patterns:

```json
{
    "Effect": "Allow",
    "Action": [
        "sns:CreateTopic",
        "sns:Publish"
    ],
    "Resource": "arn:aws:sns:us-east-1:123456789012:tenant-*"
}
```

### Tenant Isolation

Dynamic topics provide logical isolation but all topics share the same AWS account. For stronger isolation:

- Use separate AWS accounts per tenant
- Implement application-level access controls
- Audit topic access using CloudTrail

## Alternative Patterns

### Message Filtering

Instead of dynamic topics, use SNS subscription filters:

```csharp
// Single topic with message attributes
await publisher.PublishAsync(message, new PublishMetadata
{
    MessageAttributes = new Dictionary<string, MessageAttributeValue>
    {
        ["TenantId"] = new MessageAttributeValue { StringValue = "acme" }
    }
});
```

Subscribers filter using SNS filter policies. This reduces topic count but requires filter configuration.

### Queue-Per-Tenant

Use dynamic queue names instead of dynamic topics:

```csharp
config.Publications(x =>
{
    x.WithQueue<OrderCommand>(cfg =>
    {
        cfg.WithQueueName(msg =>
        {
            var cmd = (OrderCommand)msg;
            return $"tenant-{cmd.TenantId}-commands";
        });
    });
});
```

## Complete Example

### Application Configuration

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));

    config.Publications(x =>
    {
        // Dynamic topic based on tenant
        x.WithTopic<TenantEvent>(cfg =>
        {
            cfg.WithTopicName(msg =>
            {
                var evt = (TenantEvent)msg;

                // Validate tenant ID
                if (string.IsNullOrEmpty(evt.TenantId))
                    throw new ArgumentException("TenantId is required");

                return $"tenant-{evt.TenantId.ToLowerInvariant()}-events";
            });
        });
    });
});
```

### Publishing Service

```csharp
public class TenantOrderService
{
    private readonly IMessagePublisher _publisher;

    public TenantOrderService(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PlaceOrder(string tenantId, int orderId)
    {
        // Topic determined from TenantId at publish time
        await _publisher.PublishAsync(new OrderPlacedEvent
        {
            TenantId = tenantId,
            OrderId = orderId,
            Description = "New order"
        });
    }
}
```

## Troubleshooting

### "Topic name is invalid"

Ensure topic names meet AWS requirements:
- 1-256 characters
- Alphanumeric, hyphens, and underscores only
- Cannot start with "aws"

### Too many topics created

Monitor and clean up unused topics:

```bash
# List all topics
aws sns list-topics --region us-east-1

# Delete unused topics
aws sns delete-topic --topic-arn arn:aws:sns:region:account:topic-name
```

### Performance issues with topic creation

Pre-create topics using infrastructure-as-code to avoid runtime creation overhead.

## See Also

- [WithTopic](../publishing/withtopic.md) - Topic publication configuration
- [Write Configuration](../publishing/write-configuration.md) - Advanced publication options
- [Naming Conventions](../messaging-configuration/naming-conventions.md) - Default naming behavior
