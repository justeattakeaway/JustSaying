---
---

# Advanced Topics

Advanced configuration and scenarios for JustSaying. These topics cover specialized use cases and optimization techniques.

## Available Topics

### [Compression](compression.md)

Compress message bodies to reduce size and AWS costs. Learn how to configure compression thresholds and encoding options.

### [Encryption](encryption.md)

Secure messages using AWS Key Management Service \(KMS\) server-side encryption. Configure encryption for topics and queues.

### [Dynamic Topics](dynamic-topics.md)

Use dynamic topic names for multi-tenant scenarios. Route messages to tenant-specific topics based on message content.

### [Native AOT](native-aot.md)

Publish JustSaying applications with Native AOT, using a source-generated `JsonSerializerContext` for message serialization.

### [Testing](testing.md)

Testing strategies for JustSaying applications including LocalStack integration and mocking approaches.

## When to Use Advanced Features

Most applications don't need these advanced features. Consider them when:

- **Compression**: Messages frequently exceed 100KB or you want to optimize AWS costs
- **Encryption**: You have compliance requirements for data at rest
- **Dynamic Topics**: You're building multi-tenant systems with tenant isolation
- **Native AOT**: You want faster startup and a smaller footprint, for example in containers or AWS Lambda
- **Testing**: You're setting up integration tests or local development environments

## See Also

- [Publications Configuration](../publishing/configuration.md) - Basic publication setup
- [Publication Settings](../publishing/write-configuration.md) - Advanced publication options
- [Destinations](../destinations/) - Naming and configuring topics and queues
- [AWS Configuration](../aws-configuration/) - AWS client and region setup
