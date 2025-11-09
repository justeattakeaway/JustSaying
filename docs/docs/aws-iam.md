---
description: JustSaying requires some permissions to auto-create infrastructure in AWS
---

# AWS IAM

JustSaying requires the following IAM actions to run smoothly;

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
sqs:SetQueueAttributes
sqs:TagQueue
```

An example policy would look like;

```javascript
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
            "Resource": "arn:aws:sqs:aws-region:aws-account-id:uk-myfeature-orderaccepted"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sns:CreateTopic",
                "sns:Publish",
                "sns:Subscribe",
                "sns:TagResource"
            ],
            "Resource": "arn:aws:sns:aws-region:aws-account-id:uk-orderaccepted"
        }
    ]
}
```

