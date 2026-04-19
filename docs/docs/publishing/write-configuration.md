---
---

# Write Configuration

Configure topic and queue publication behavior using `WithWriteConfiguration`. These settings control encryption, compression, message retention, and error handling for published messages.

## Configuration Usage

Write configuration is accessed through the publication builders:

```csharp
config.Publications(x =>
{
    // For topics
    x.WithTopic<OrderPlacedEvent>(cfg =>
    {
        cfg.WithWriteConfiguration(w =>
        {
            // Configure SNS write settings
        });
    });

    // For queues
    x.WithQueue<ProcessPaymentCommand>(cfg =>
    {
        cfg.WithWriteConfiguration(w =>
        {
            // Configure SQS write settings
        });
    });
});
```

Note that these configuration options are per topic or queue.

## Topic Write Configuration (SNS)

### Encryption

#### `Encryption`

Configures server-side encryption using AWS Key Management Service \(KMS\). Provide a `ServerSideEncryption` object with the KMS Key ID.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.Encryption = new ServerSideEncryption
    {
        KmsMasterKeyId = "arn:aws:kms:us-east-1:123456789012:key/your-key-id"
    };
});
```

See [Encryption](../advanced/encryption.md) for more details and required IAM permissions.

### Compression

#### `CompressionOptions`

Compresses message bodies to reduce size and AWS costs. Specify a `PublishCompressionOptions` object with compression encoding and threshold.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.CompressionOptions = new PublishCompressionOptions
    {
        CompressionEncoding = ContentEncodings.GzipBase64,
        MessageLengthThreshold = 100_000 // Compress messages > 100KB
    };
});
```

See [Compression](../advanced/compression.md) for more details.

### Error Handling

#### `HandleException`

Provides a custom exception handler for publish failures. Return `true` to mark the exception as handled, or `false` to rethrow it.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.HandleException = (exception, message) =>
    {
        // Log the error
        Console.WriteLine($"Failed to publish message {message.Id}: {exception.Message}");

        // Return false to rethrow
        return false;
    };
});
```

### Subject

#### `Subject`

Sets a custom SNS message subject. By default, JustSaying uses the message type name.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.Subject = "Custom Subject for All Messages";
});
```

### Raw Message Delivery

#### `IsRawMessage`

When `true`, publishes messages without the JustSaying envelope. Use this for interoperability with non-JustSaying systems.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.IsRawMessage = true;
});
```

## Queue Write Configuration (SQS)

Queue write configuration includes all the topic configuration options plus additional SQS-specific settings.

### Encryption

#### `WithEncryption(string masterKeyId)`

Configures server-side encryption for the SQS queue using a KMS key ID.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.WithEncryption("your-kms-key-id");
});
```

#### `WithEncryption(ServerSideEncryption encryption)`

Configures server-side encryption using a `ServerSideEncryption` object.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.WithEncryption(new ServerSideEncryption
    {
        KmsMasterKeyId = "arn:aws:kms:us-east-1:123456789012:key/your-key-id"
    });
});
```

### Error Queue

#### `WithErrorQueue()`

Specifies that an error queue should be created for this publication. This is the default behavior.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.WithErrorQueue();
});
```

#### `WithNoErrorQueue()`

Specifies that no error queue should be created for this publication.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.WithNoErrorQueue();
});
```

#### `WithErrorQueueOptOut(bool value)`

Explicitly opts in or out of error queue creation. `WithErrorQueue` and `WithNoErrorQueue` delegate to this method.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.WithErrorQueueOptOut(shouldOptOut: true); // No error queue
});
```

### Message Retention

#### `WithMessageRetention(TimeSpan value)`

Specifies how long messages should be kept in the queue before being automatically deleted. The default is 4 days. AWS allows retention from 60 seconds to 14 days.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.WithMessageRetention(TimeSpan.FromDays(7));
});
```

### Visibility Timeout

#### `WithVisibilityTimeout(TimeSpan value)`

Specifies how long a message should be invisible to other consumers after being retrieved. The default is 30 seconds. For messages that take a long time to process, increase this value to avoid duplicate handling.

```csharp
cfg.WithWriteConfiguration(w =>
{
    w.WithVisibilityTimeout(TimeSpan.FromMinutes(5));
});
```

## Complete Examples

### Topic with Encryption and Compression

```csharp
config.Publications(x =>
{
    x.WithTopic<LargeSecureEvent>(cfg =>
    {
        cfg.WithWriteConfiguration(w =>
        {
            // Encrypt at rest
            w.Encryption = new ServerSideEncryption
            {
                KmsMasterKeyId = "your-kms-key-id"
            };

            // Compress large messages
            w.CompressionOptions = new PublishCompressionOptions
            {
                CompressionEncoding = ContentEncodings.GzipBase64,
                MessageLengthThreshold = 50_000
            };
        });
    });
});
```

### Queue with Custom Retention and No Error Queue

```csharp
config.Publications(x =>
{
    x.WithQueue<TemporaryCommand>(cfg =>
    {
        cfg.WithWriteConfiguration(w =>
        {
            // Keep messages for only 1 hour
            w.WithMessageRetention(TimeSpan.FromHours(1));

            // Don't create an error queue
            w.WithNoErrorQueue();

            // Long visibility timeout for slow processing
            w.WithVisibilityTimeout(TimeSpan.FromMinutes(10));
        });
    });
});
```

## Advanced Topics

For more information on specific configuration topics, see:

- [Compression](../advanced/compression.md) - Reducing message size and costs
- [Encryption](../advanced/encryption.md) - Securing messages with AWS KMS
- [Dynamic Topics](../advanced/dynamic-topics.md) - Multi-tenant message routing
