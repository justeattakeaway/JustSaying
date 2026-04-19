---
---

# Compression

Message compression reduces the size of message bodies before publishing, lowering AWS costs and improving throughput. JustSaying supports Gzip compression with Base64 encoding.

## Why Compress Messages

Compress messages when:
- Message bodies frequently exceed 100KB
- You want to reduce AWS SNS/SQS costs
- Network bandwidth is a concern
- You're approaching the 256KB SNS message size limit

## Configuration

Configure compression using `WithWriteConfiguration` on topic or queue publications:

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));

    config.Publications(x =>
    {
        x.WithTopic<LargeDataEvent>(cfg =>
        {
            cfg.WithWriteConfiguration(w =>
            {
                w.CompressionOptions = new PublishCompressionOptions
                {
                    CompressionEncoding = ContentEncodings.GzipBase64,
                    MessageLengthThreshold = 100_000 // Compress if > 100KB
                };
            });
        });
    });
});
```

## Compression Options

### CompressionEncoding

Specifies the compression algorithm. Currently, only `ContentEncodings.GzipBase64` is supported:

```csharp
w.CompressionOptions = new PublishCompressionOptions
{
    CompressionEncoding = ContentEncodings.GzipBase64
};
```

This uses Gzip compression with Base64 encoding for safe transport through SNS/SQS.

### MessageLengthThreshold

Specifies the minimum message size \(in bytes\) before compression is applied. Messages smaller than this threshold are not compressed.

```csharp
w.CompressionOptions = new PublishCompressionOptions
{
    CompressionEncoding = ContentEncodings.GzipBase64,
    MessageLengthThreshold = 50_000 // Only compress messages > 50KB
};
```

**Recommended Thresholds**:
- `50_000` (50KB) - Aggressive compression for cost savings
- `100_000` (100KB) - Balanced approach for large messages
- `200_000` (200KB) - Only compress near SNS limit (256KB)

## How It Works

1. **Publisher**: JustSaying compresses the message body if it exceeds the threshold
2. **Message Attributes**: A `Content-Encoding` attribute is added indicating the compression type
3. **Subscriber**: JustSaying automatically detects the compression and decompresses the message
4. **Transparency**: Handlers receive the decompressed message automatically

## Complete Example

### Publisher Configuration

```csharp
config.Publications(x =>
{
    x.WithTopic<OrderDetailsEvent>(cfg =>
    {
        cfg.WithWriteConfiguration(w =>
        {
            w.CompressionOptions = new PublishCompressionOptions
            {
                CompressionEncoding = ContentEncodings.GzipBase64,
                MessageLengthThreshold = 100_000
            };
        });
    });
});
```

### Subscriber Configuration

Subscribers automatically decompress messages - no configuration needed:

```csharp
config.Subscriptions(x =>
{
    x.ForTopic<OrderDetailsEvent>();
});

services.AddJustSayingHandler<OrderDetailsEvent, OrderDetailsEventHandler>();
```

### Handler

Handlers receive the decompressed message:

```csharp
public class OrderDetailsEventHandler : IHandlerAsync<OrderDetailsEvent>
{
    public Task<bool> Handle(OrderDetailsEvent message)
    {
        // Message is automatically decompressed
        Console.WriteLine($"Received order: {message.OrderId}");
        return Task.FromResult(true);
    }
}
```

## Performance Considerations

### Benefits

- **Reduced AWS Costs**: Smaller messages mean lower data transfer and storage costs
- **Higher Throughput**: More messages can fit within AWS limits
- **Avoid Size Limits**: Compress large messages to stay under the 256KB SNS limit

### Trade-offs

- **CPU Overhead**: Compression and decompression require CPU time
- **Latency**: Additional processing time for compression/decompression
- **Complexity**: Debugging compressed messages is more difficult

## When to Use Compression

### Good Use Cases

- Large JSON payloads with repeated data
- Messages with text or structured data that compresses well
- High-volume scenarios where cost savings matter
- Messages approaching AWS size limits

### Poor Use Cases

- Small messages (less than 10KB) - compression overhead isn't worth it
- Already compressed data (images, videos) - won't compress further
- Low-volume scenarios - cost savings minimal

## Compression Ratios

Typical compression ratios for different message types:

| Message Type | Compression Ratio |
|--------------|-------------------|
| Structured JSON | 60-80% size reduction |
| Repeated data | 70-90% size reduction |
| Random strings | 10-30% size reduction |
| Binary data | Minimal or none |

## Interoperability

JustSaying's compression is transparent to other JustSaying applications. However, non-JustSaying subscribers must:

1. Check for the `Content-Encoding` message attribute
2. Detect `gzip-base64` encoding
3. Base64 decode the message body
4. Gzip decompress the result
5. Parse the JSON message

For interoperability with non-JustSaying systems, consider using uncompressed messages or documenting the compression format.

## Troubleshooting

### "Failed to decompress message"

This error occurs when a compressed message cannot be decompressed. Possible causes:

- Message was corrupted during transmission
- Wrong compression encoding specified
- Message attribute indicating compression is missing

### Messages are compressed unexpectedly

Check your `MessageLengthThreshold` setting. Lower thresholds cause more messages to be compressed.

### Compression not working

Verify:
1. `CompressionOptions` is configured on the publication
2. Message size exceeds `MessageLengthThreshold`
3. Both publisher and subscriber are using compatible JustSaying versions

## See Also

- [Write Configuration](../publishing/write-configuration.md) - Complete write configuration options
- [Publications Configuration](../publishing/configuration.md) - Basic publication setup
