---
---

# Compression

Message compression reduces the size of message bodies before publishing, lowering AWS costs and improving throughput. JustSaying supports Gzip compression with Base64 encoding.

## Message size limits

Both services accept payloads of up to 1 MiB, but they get there differently, and that difference drives
how JustSaying models compression:

| Destination | Maximum message size | Opt-in? |
|-------------|----------------------|---------|
| SQS queue | 1 MiB (1,048,576 bytes) by default, can be set lower | No, this is the default |
| SNS topic | 256 KiB (262,144 bytes) by default, up to 1 MiB | Yes, via the `MaximumMessageSize` topic attribute |

So an SQS queue takes a 1 MiB message with no configuration at all, whereas an SNS topic has to be
told to. See [Publishing large messages with Amazon SNS](https://docs.aws.amazon.com/sns/latest/dg/large-message-payloads.html).

The one catch with queues is that `MaximumMessageSize` is a per-queue attribute, and a queue that has had
it set explicitly keeps that value. Queues JustSaying creates never set it, so they get 1 MiB, but some
infrastructure tooling still defaults it to 256 KiB (Terraform's `max_message_size`, for one). See
[MaximumMessageSize](#maximummessagesize) for how to tell JustSaying about a queue like that.

## Why Compress Messages

Compress messages when:
- Message bodies frequently exceed 100KB
- You want to reduce AWS SNS/SQS costs
- Network bandwidth is a concern
- You're approaching the destination's message size limit

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

This is a question of when compressing is worth the CPU, and is separate from how large a message the
destination will accept. Leave it unset and JustSaying derives it from the destination's maximum message
size, leaving 2KB of headroom:

- 254KB for an SNS topic on the default 256KB limit
- 1022KB for an SQS queue, or for a topic whose `MaximumMessageSize` has been raised to 1 MiB

**Recommended Thresholds**:
- `50_000` (50KB) - Aggressive compression for cost savings
- `100_000` (100KB) - Balanced approach for large messages
- unset - Only compress when the message is close to being rejected

A message that is too large for the destination is always compressed, whatever the threshold says. So
a threshold set above the destination's limit (easily done when one `DefaultCompressionOptions` is shared
between 1 MiB queues and 256KB topics) won't cause a message that could have been made to fit to be rejected.

The message attributes count towards the size, the same way AWS counts them. Note that compression only
shrinks the body, so a small body with large attributes may not compress to anything smaller — JustSaying
keeps the uncompressed body when compressing would not have helped.

### MaximumMessageSize

Raises the size limit on an SNS topic above the 256KB default. JustSaying applies it as the topic's
`MaximumMessageSize` attribute when it creates the topic, and uses it as the budget for compression and
for packing batches:

```csharp
x.WithTopic<LargeDataEvent>(cfg =>
{
    cfg.WithWriteConfiguration(w =>
    {
        w.MaximumMessageSize = 1024 * 1024; // 1 MiB
    });
});
```

A topic with this set above 256KB supports only SQS, Amazon Data Firehose and Lambda subscriptions, and
at most 100 subscriptions in total.

When you publish to a topic by ARN rather than letting JustSaying create it, JustSaying has no way to
know the topic has been raised, so tell it:

```csharp
x.WithTopicArn<LargeDataEvent>(topicArn, cfg => cfg.WithMaximumMessageSize(1024 * 1024));
```

Queues rarely need this, because SQS already defaults to 1 MiB. The exception is a queue whose
`MaximumMessageSize` attribute has been set lower by whatever created it, where JustSaying would
otherwise assume 1 MiB and leave messages uncompressed that the queue then rejects. Unlike the topic
setting this only describes the queue, JustSaying does not apply it as a queue attribute:

```csharp
x.WithQueue<LargeDataEvent>(cfg =>
{
    cfg.WithWriteConfiguration(w =>
    {
        w.MaximumMessageSize = 256 * 1024; // 256 KiB
    });
});

x.WithQueueArn<LargeDataEvent>(queueArn, cfg => cfg.WithMaximumMessageSize(256 * 1024));
```

If a message still exceeds the limit after compression, JustSaying throws a `MessageTooLargeException`
rather than letting AWS reject the publish with an opaque `InvalidParameter` error.

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
- **Avoid Size Limits**: Compress large messages to stay under the destination's limit

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
