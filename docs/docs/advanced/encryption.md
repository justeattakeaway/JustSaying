---
---

# Encryption

JustSaying supports server-side encryption for messages using AWS Key Management Service \(KMS\). Encrypt messages at rest in SNS topics and SQS queues to meet compliance and security requirements.

## Why Use Encryption

Use encryption when:
- Compliance requirements mandate encryption at rest
- Messages contain sensitive data \(PII, financial data, etc.\)
- Industry regulations require encrypted messaging \(HIPAA, PCI-DSS, etc.\)
- Security policies require encryption for all data

## Configuration

Encryption is a setting of the topic or queue, so it's configured on its [destination](/destinations/) with `WithEncryption`. It's applied when JustSaying creates the resource, and added to an existing one on startup.

### Topic Encryption (SNS)

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));

    config.Publications(x =>
    {
        x.WithTopic<SecureOrderEvent>(TopicDestination.ByConvention(t => t
            .WithEncryption("arn:aws:kms:us-east-1:123456789012:key/your-key-id")));
    });
});
```

### Queue Encryption (SQS)

```csharp
config.Publications(x =>
{
    x.WithQueue<SecurePaymentCommand>(QueueDestination.ByConvention(q => q
        .WithEncryption("your-kms-key-id")));
});
```

A subscription's queue is configured the same way, through `WithQueue` on `ForTopic`, or the destination of `ForQueue`:

```csharp
config.Subscriptions(x =>
{
    x.ForTopic<SecureOrderEvent>(c => c
        .WithQueue(QueueDestination.ByConvention(q => q.WithEncryption("alias/my-app-key"))));
});
```

A queue's `_error` queue is encrypted with the same key.

Encryption is never removed by JustSaying: a destination without `WithEncryption` leaves an existing queue's or topic's encryption as it is. To decrypt a queue on purpose, change it outside JustSaying. See [Infrastructure on Startup](/destinations/startup-behaviour).

## KMS Key Specification

`WithEncryption` takes the KMS key in any of the formats KMS accepts:

```csharp
q.WithEncryption("1234abcd-12ab-34cd-56ef-1234567890ab");                                         // Key ID
q.WithEncryption("arn:aws:kms:us-east-1:123456789012:key/1234abcd-12ab-34cd-56ef-1234567890ab"); // Key ARN
q.WithEncryption("alias/my-app-key");                                                             // Alias name
q.WithEncryption("arn:aws:kms:us-east-1:123456789012:alias/my-app-key");                          // Alias ARN
```

For a queue, the `ServerSideEncryption` overload also sets how long SQS reuses a data key:

```csharp
q.WithEncryption(new ServerSideEncryption
{
    KmsMasterKeyId = "alias/my-app-key",
    KmsDataKeyReusePeriod = TimeSpan.FromMinutes(5),
});
```

## IAM Permissions

Applications using encryption require additional IAM permissions. See [AWS IAM](../aws-iam.md) for complete details.

### Publisher Permissions

Publishers need `kms:GenerateDataKey` to encrypt messages:

```json
{
    "Effect": "Allow",
    "Action": [
        "kms:GenerateDataKey"
    ],
    "Resource": "arn:aws:kms:us-east-1:123456789012:key/your-key-id"
}
```

### Subscriber Permissions

Subscribers need `kms:Decrypt` to decrypt messages:

```json
{
    "Effect": "Allow",
    "Action": [
        "kms:Decrypt"
    ],
    "Resource": "arn:aws:kms:us-east-1:123456789012:key/your-key-id"
}
```

## Complete Example

### Publisher Application

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));

    config.Publications(x =>
    {
        // Encrypt using KMS
        x.WithTopic<PatientRecordEvent>(TopicDestination.ByConvention(t => t
            .WithEncryption(configuration["AWS:KMS:KeyId"])));
    });
});

// Publish encrypted message
var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
await publisher.PublishAsync(new PatientRecordEvent
{
    PatientId = 12345,
    MedicalData = "Sensitive information"
});
```

### Subscriber Application

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));

    config.Subscriptions(x =>
    {
        // Automatic decryption - no encryption config needed
        x.ForTopic<PatientRecordEvent>();
    });
});

services.AddJustSayingHandler<PatientRecordEvent, PatientRecordEventHandler>();
```

### Handler

Handlers receive decrypted messages automatically:

```csharp
public class PatientRecordEventHandler : IHandlerAsync<PatientRecordEvent>
{
    public Task<bool> Handle(PatientRecordEvent message)
    {
        // Message is automatically decrypted
        Console.WriteLine($"Processing patient {message.PatientId}");
        return Task.FromResult(true);
    }
}
```

## KMS Key Management

### Creating a KMS Key

Create a KMS key using AWS CLI:

```bash
aws kms create-key \
    --description "JustSaying message encryption" \
    --key-usage ENCRYPT_DECRYPT

aws kms create-alias \
    --alias-name alias/justsaying-messages \
    --target-key-id <key-id-from-previous-command>
```

### Key Policies

Ensure your KMS key policy allows the application to use it:

```json
{
    "Sid": "Allow application to use the key",
    "Effect": "Allow",
    "Principal": {
        "AWS": "arn:aws:iam::123456789012:role/YourApplicationRole"
    },
    "Action": [
        "kms:Decrypt",
        "kms:GenerateDataKey"
    ],
    "Resource": "*"
}
```

## How It Works

1. **Publisher**: JustSaying requests a data encryption key from KMS
2. **Encryption**: Message body is encrypted using the data key
3. **Storage**: Encrypted message is stored in SNS/SQS
4. **Subscriber**: JustSaying requests decryption from KMS
5. **Decryption**: Message is decrypted and delivered to the handler

All encryption and decryption is transparent to application code.

## Performance Considerations

### Latency

- Each encryption operation calls KMS, adding latency
- Decryption also requires KMS API calls
- KMS has rate limits that may affect high-throughput applications

### Cost

- KMS charges per API call ($0.03 per 10,000 requests)
- Data key caching reduces KMS calls but adds complexity
- For high-volume scenarios, consider the cost impact

### Caching

AWS SDK caches data encryption keys to reduce KMS calls. Cache duration is typically 5 minutes but can be configured at the SDK level.

## Security Best Practices

1. **Use Separate Keys**: Use different KMS keys for different sensitivity levels
2. **Rotate Keys**: Enable automatic key rotation in KMS
3. **Audit**: Enable CloudTrail logging for KMS operations
4. **Least Privilege**: Grant only necessary KMS permissions
5. **Regional Keys**: Use region-specific keys to meet compliance requirements

## Troubleshooting

### "Access denied" when publishing

Your IAM role lacks `kms:GenerateDataKey` permission. Add the permission for your KMS key:

```json
{
    "Effect": "Allow",
    "Action": ["kms:GenerateDataKey"],
    "Resource": "arn:aws:kms:region:account:key/key-id"
}
```

### "Access denied" when subscribing

Your IAM role lacks `kms:Decrypt` permission. Add the permission for your KMS key.

### Messages not encrypting

Verify:
1. `WithEncryption()` is configured on the topic or queue's destination
2. KMS key ID is valid and accessible
3. IAM role has required permissions

## See Also

- [AWS IAM](../aws-iam.md) - Required KMS permissions
- [Destinations](../destinations/) - Topic and queue settings
- [AWS KMS Documentation](https://docs.aws.amazon.com/kms/) - KMS best practices
