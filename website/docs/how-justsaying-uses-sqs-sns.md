---
description: Lower level details on which API's JustSaying calls to achieve its mission
---

# How JustSaying Uses SQS/SNS

### Publishing to SNS

JustSaying uses the SNS Publish API to send messages to SNS \(documented [here](https://docs.aws.amazon.com/sns/latest/api/API_Publish.html)\). The following parameters are set:

**TopicArn**

The ARN of the topic to which the message is being published.

**Subject**

JustSaying will set this to the type name of the .NET message class - "OrderAccepted", for example.

Since a JustSaying _subscriber_ relies on this string to know what type to deserialise the JSON in the Message parameter into, non-JustSaying _publishers_ should restrict this parameter to alphanumeric characters.

**MessageAttributes**

Any message attributes that you have chosen to send along with the message.

**Message**

The actual message object, serialised to JSON. It is worth noting that the JSON sent in this parameter becomes a subset of the JSON that will be delivered \(by the SNS service\) to an SQS subscriber. This is described [here](https://docs.aws.amazon.com/sns/latest/dg/sns-sqs-as-subscriber.html) and [here](https://docs.aws.amazon.com/sns/latest/dg/sns-message-and-json-formats.html#http-notification-json).

In addition to those properties you will define in your own messages, the following are available to a JustSaying publisher:

| Property | Type | Description | Populated by default? |
| :--- | :--- | :--- | :---: |
| Id | GUID | A random GUID which uniquely identifies the message. | Yes |
| TimeStamp | DateTime | The current time, in UTC, in [ISO 8601 format](https://en.wikipedia.org/wiki/ISO_8601). | Yes |
| RaisingComponent | string | A string which identifies the publishing application - "orderapi", for example. | No |
| Version | string | The message version - "2", for example. | No |
| SourceIp | string | The IP address of the machine which published the message. | No |
| Tenant | string | In a multi-tenant architecture, a string to identify the tenant for which the message is applicable. | No |
| Conversation | string | A string which can be used to correlate multiple messages \(to identify messages belonging to a single operation/customer journey, for example\). | No |

Properties which are not populated will not appear in the message JSON. Enumerations are serialised as strings.

In summary, suppose we have the following message class:

```csharp
public class OrderAccepted : Message
{
    public int OrderId { get; set; }
}
```

And suppose I create a message object from that class:

```csharp
var orderAccepted = new OrderAccepted
{
    OrderId = 1234,
    RaisingComponent = "orderapi",
};
```

This will result in the following JSON being created and populated on that Message parameter to the SNS Publish API:

```javascript
{
    "OrderId": 1234,
    "Id": "e3f84a55-b677-43df-8c24-92c170fdd89f",
    "TimeStamp": "2019-07-03T09:53:09.2956149Z",
    "RaisingComponent": "orderapi"
}
```



### Sending to SQS \("point to point"\)

JustSaying uses the SQS SendMessage API to send messages to SQS \(documented [here](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessage.html)\). The following parameters are set:

**QueueUrl**

The URL of the queue to which the message is being sent.

**DelaySeconds**

JustSaying will set this value to be that of the `Delay` property on the `PublishMetadata` class which is supplied as a parameter to the `PublishAsync` method.

**MessageBody**

A JSON object containing two properties, `Subject` and `Message`. `Subject`, as with the SNS publisher, will be the type name of the .NET message class. `Message` will be the actual message object, serialised to JSON. The same serialisation applies as for the SNS publisher.

As an example, if I create the same OrderAccepted object as before and send that to SQS, then the value of this parameter will be:

```javascript
{
    "Subject": "OrderAccepted",
    "Message": "{\"OrderId\":1234,\"Id\":\"e3f84a55-b677-43df-8c24-92c170fdd89f\",\"TimeStamp\":\"2019-07-03T09:53:09.2956149Z\",\"RaisingComponent\":\"orderapi\"}"
}
```

This structure is to ensure that messages that are delivered to SQS queues as a result of directly publishing to the queue have the same format as messages delivered to a queue as a result of being subscribed to an SNS topic to which a message was published. So, from the point of view of a subscriber, whether the message was originally delivered to SNS or SQS is not important.

