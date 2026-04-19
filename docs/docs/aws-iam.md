---
description: JustSaying requires IAM permissions to create infrastructure and publish/consume messages in AWS
---

# AWS IAM

JustSaying requires specific IAM permissions depending on your use case. This page outlines the minimum permissions needed for different scenarios.

## Full Permissions (Publishing and Subscribing)

If your application both publishes and subscribes to messages, the following permissions are required:

```text
// SNS
sns:CreateTopic
sns:ListTopics
sns:Publish
sns:SetSubscriptionAttributes
sns:Subscribe
sns:TagResource

// SQS
sqs:ChangeMessageVisibility
sqs:CreateQueue
sqs:DeleteMessage
sqs:GetQueueAttributes
sqs:GetQueueUrl
sqs:ListQueues
sqs:ReceiveMessage
sqs:SendMessage
sqs:SetQueueAttributes
sqs:TagQueue
```

### Example IAM Policy

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "sqs:ListQueues",
                "sns:ListTopics",
                "sns:SetSubscriptionAttributes"
            ],
            "Resource": "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sqs:ChangeMessageVisibility",
                "sqs:CreateQueue",
                "sqs:DeleteMessage",
                "sqs:GetQueueUrl",
                "sqs:GetQueueAttributes",
                "sqs:ReceiveMessage",
                "sqs:SendMessage",
                "sqs:SetQueueAttributes",
                "sqs:TagQueue"
            ],
            "Resource": "arn:aws:sqs:aws-region:aws-account-id:*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sns:CreateTopic",
                "sns:Publish",
                "sns:Subscribe",
                "sns:TagResource"
            ],
            "Resource": "arn:aws:sns:aws-region:aws-account-id:*"
        }
    ]
}
```

## Publisher-Only Permissions

If your application only publishes messages without subscribing, you need fewer permissions:

### SNS Topic Publishing

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "sns:ListTopics"
            ],
            "Resource": "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sns:CreateTopic",
                "sns:Publish",
                "sns:TagResource"
            ],
            "Resource": "arn:aws:sns:aws-region:aws-account-id:*"
        }
    ]
}
```

### SQS Queue Publishing

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "sqs:ListQueues"
            ],
            "Resource": "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sqs:CreateQueue",
                "sqs:GetQueueUrl",
                "sqs:GetQueueAttributes",
                "sqs:SendMessage",
                "sqs:SetQueueAttributes",
                "sqs:TagQueue"
            ],
            "Resource": "arn:aws:sqs:aws-region:aws-account-id:*"
        }
    ]
}
```

## Subscriber-Only Permissions

If your application only subscribes to messages without publishing, you need these permissions:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "sqs:ListQueues",
                "sns:ListTopics",
                "sns:SetSubscriptionAttributes"
            ],
            "Resource": "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sqs:ChangeMessageVisibility",
                "sqs:CreateQueue",
                "sqs:DeleteMessage",
                "sqs:GetQueueUrl",
                "sqs:GetQueueAttributes",
                "sqs:ReceiveMessage",
                "sqs:SetQueueAttributes",
                "sqs:TagQueue"
            ],
            "Resource": "arn:aws:sqs:aws-region:aws-account-id:*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sns:CreateTopic",
                "sns:Subscribe",
                "sns:TagResource"
            ],
            "Resource": "arn:aws:sns:aws-region:aws-account-id:*"
        }
    ]
}
```

## Encryption Permissions

If you use server-side encryption with AWS KMS for topics or queues, additional permissions are required:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "kms:Decrypt",
                "kms:GenerateDataKey"
            ],
            "Resource": "arn:aws:kms:aws-region:aws-account-id:key/your-key-id"
        }
    ]
}
```

### Publisher with Encryption

Publishers need `kms:GenerateDataKey` to encrypt messages:

```json
{
    "Effect": "Allow",
    "Action": [
        "kms:GenerateDataKey"
    ],
    "Resource": "arn:aws:kms:aws-region:aws-account-id:key/your-key-id"
}
```

### Subscriber with Encryption

Subscribers need `kms:Decrypt` to decrypt messages:

```json
{
    "Effect": "Allow",
    "Action": [
        "kms:Decrypt"
    ],
    "Resource": "arn:aws:kms:aws-region:aws-account-id:key/your-key-id"
}
```

## Restricting Permissions by Resource

For production environments, restrict permissions to specific resources instead of using wildcards (`*`):

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "sns:CreateTopic",
                "sns:Publish",
                "sns:TagResource"
            ],
            "Resource": [
                "arn:aws:sns:us-east-1:123456789012:order-*",
                "arn:aws:sns:us-east-1:123456789012:payment-*"
            ]
        },
        {
            "Effect": "Allow",
            "Action": [
                "sqs:CreateQueue",
                "sqs:SendMessage",
                "sqs:ReceiveMessage",
                "sqs:DeleteMessage",
                "sqs:GetQueueAttributes",
                "sqs:SetQueueAttributes",
                "sqs:TagQueue"
            ],
            "Resource": [
                "arn:aws:sqs:us-east-1:123456789012:order-*",
                "arn:aws:sqs:us-east-1:123456789012:payment-*"
            ]
        }
    ]
}
```

## Pre-Existing Resources

If topics and queues are created outside of JustSaying (by infrastructure-as-code tools like CloudFormation or Terraform), you can reduce permissions by removing the `Create*` and `SetAttributes` actions:

### Minimal Publisher Permissions (Pre-Existing Resources)

```json
{
    "Effect": "Allow",
    "Action": [
        "sns:Publish"
    ],
    "Resource": "arn:aws:sns:aws-region:aws-account-id:*"
}
```

### Minimal Subscriber Permissions (Pre-Existing Resources)

```json
{
    "Effect": "Allow",
    "Action": [
        "sqs:ReceiveMessage",
        "sqs:DeleteMessage",
        "sqs:ChangeMessageVisibility"
    ],
    "Resource": "arn:aws:sqs:aws-region:aws-account-id:*"
}
```

## See Also

- [Credentials](aws-configuration/credentials.md) - Configuring AWS credentials
- [Encryption](advanced/encryption.md) - Server-side encryption configuration
- [AWS IAM Documentation](https://docs.aws.amazon.com/IAM/latest/UserGuide/) - AWS IAM best practices

