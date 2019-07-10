# JustSaying

[![NuGet](https://img.shields.io/nuget/v/JustSaying.svg?maxAge=3600)](https://www.nuget.org/packages/JustSaying/)
[![Gitter](https://img.shields.io/gitter/room/justeat/JustSaying.js.svg?maxAge=2592000)](https://gitter.im/justeat/JustSaying)

[![Build status](https://ci.appveyor.com/api/projects/status/vha51pup5lcnesu3/branch/master?svg=true)](https://ci.appveyor.com/project/justeattech/justsaying)
[![codecov](https://codecov.io/gh/justeat/JustSaying/branch/master/graph/badge.svg)](https://codecov.io/gh/justeat/JustSaying)

A helpful library for publishing and consuming events / messages over SNS (SNS / SQS as a message bus).

## Getting started

Before you can start publishing or consuming messages, you want to configure the AWS client factory.

````c#
        CreateMeABus.DefaultClientFactory = () => new DefaultAwsClientFactory(new BasicAWSCredentials("accessKey", "secretKey"))
````

You will also need an `ILoggerFactory` ([see here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging)); if you do not want logging then you can use an empty logger factory like so:

````c#
    ILoggerFactory loggerFactory = NullLoggerFactory.Instance;
````

## Publishing messages

Here's how to get up & running with simple message publishing.

### 1. Create a message object (POCO)

* These can be as complex as you like (provided it is under 256k serialised as Json).
* They must be derived from the abstract Message class (currently).

````c#
        public class OrderAccepted : Message
        {
            public OrderAccepted(int orderId)
            {
                OrderId = orderId;
            }
            public int OrderId { get; private set; }
        }
````

### 2. Registering publishers

* You will need to tell JustSaying which messages you intend to publish so it can setup any missing topics for you.
* In this case, we are telling it to publish the OrderAccepted messages.
* The topic will be the message type.

````c#
          var publisher = CreateMeABus.WithLogging(loggerFactory)
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithSnsMessagePublisher<OrderAccepted>();
````

### 2.(a) Configuring publishing options

* You can also specify some publishing options (such as how to handle failures) using a configuration object like so:

````c#
         CreateMeABus.WithLogging(loggerFactory)
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .ConfigurePublisherWith(c => { c.PublishFailureReAttempts = 3; c.PublishFailureBackoffMilliseconds = 50; })
                .WithSnsMessagePublisher<OrderAccepted>();
````

### 3. Publish a message

* This can be done wherever you want within your application.
* Simply pass the publisher object through using your IOC container.
* In this case, we are publishing the fact that a given order has been accepted.

````c#
        publisher.Publish(new OrderAccepted(123456));
````

BOOM! You're done publishing!

## Consuming messages

Here's how to get up & running with message consumption.
We currently support SQS subscriptions only, but keep checking back for other methods too (HTTP, Kinesis)
(although we are kinda at the mercy of AWS here for internal HTTP delivery...)

### 1. Create Handlers

* We tell the stack to handle messages by implementing an interface which tells the handler our message type
* Here, we're creating a handler for OrderAccepted messages.
* This is where you pass on to your BLL layer.
* We also need to tell the stack whether we handled the message as expected. We can say things got messy either by returning false, or bubbling up exceptions.

````c#
        public class OrderNotifier : IHandler<OrderAccepted>
        {
            public bool Handle(OrderAccepted message)
            {
                // Some logic here ...
                // e.g. bll.NotifyRestaurantAboutOrder(message.OrderId);
                return true;
            }
        }
````

### 2. Register a subscription

* This can be done at the same time as your publications are set up.
* There is no limit to the number of handlers you can add to a subscription.
* You can specify message retention policies etc in your subscription for resiliency purposes.
* In this case, we are telling JustSaying to keep 'OrderAccepted' messages for the default time, which is one minute. They will be thrown away if not handled in this time.
* We are telling it to keep 'OrderFailed' messages for 1 minute and not to handle them again on failure for 30 seconds. These are the default values.

````c#
            CreateMeABus.WithLogging(loggerFactory)
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithSqsTopicSubscriber()
                .IntoQueue("CustomerOrders")
                .WithMessageHandler<OrderAccepted>(new OrderNotifier())
                .StartListening();
````

That's it. By calling `StartListening()` we are telling the stack to begin polling SQS for incoming messages.

### 2.(a) Subscription Configuration

* In this case, we are telling JustSaying to keep 'OrderAccepted' messages for the default time, which is one minute. They will be thrown away if not handled in this time.
* We are telling it to keep 'OrderFailed' messages for 5 mins, and not to handle them again on failure for 60 seconds

````c#
            CreateMeABus.WithLogging(loggerFactory)
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithSqsTopicSubscriber()
                .IntoQueue("CustomerOrders")
                    .ConfigureSubscriptionWith(c => { c.MessageRetentionSeconds = 60; })
                        .WithMessageHandler<OrderAccepted>(new NotifyCustomerOfAcceptedOrder())
                    .ConfigureSubscriptionWith(c => { c.MessageRetentionSeconds = 300; c.VisibilityTimeoutSeconds = 60; })
                        .WithMessageHandler<OrderFailed>(new NotifyCustomerOfFailedOrder())
                .StartListening();
````

### 2.(b) Configure Throttling

JustSaying throttles message handlers, which means JustSaying will limit the maximum number of messages being processed concurrently. The default limit is 8 threads per [processor core](https://msdn.microsoft.com/en-us/library/system.environment.processorcount.aspx), i.e. `Environment.ProcessorCount * 8`.
We feel that this is a sensible number, but it can be overridden. This is useful for web apps with TCP thread restrictions.
To override throttling you need to specify optional parameter when setting SqsTopicSubcriber

````c#

            .ConfigureSubscriptionWith(c => { c.MaxAllowedMessagesInFlight = 100; })
                .WithMessageHandler<OrderAccepted>(new NotifyCustomerOfAcceptedOrder())

````

### 2.(c) Control Handlers' life cycle

You can tell JustSaying to delegate the creation of your handlers to an IoC container. All you need to do is to implement IHandlerResolver interface and pass it along when registering your handlers.

````c#
CreateMeABus.WithLogging(loggerFactory)
            .InRegion(RegionEndpoint.EUWest1.SystemName)
            .WithSqsTopicSubscriber()
            .IntoQueue("CustomerOrders")
            .WithMessageHandler<OrderAccepted>(new HandlerResolver())
````

## Interrogation

JustSaying provides you access to the Subscribers and Publishers message types via ````IAmJustInterrogating```` interface on the message bus.

```c#

            IAmJustSayingFluently bus = CreateMeABus.WithLogging(loggerFactory)
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithSnsMessagePublisher<OrderAccepted>();

            IInterrogationResponse response =((IAmJustInterrogating)bus).WhatDoIHave();
```

## Logging

JustSaying stack will throw out the following named logs from NLog:

* "JustSaying"
        * Information on the setup & your configuration (Info level). This includes all subscriptions, tenants, publication registrations etc.
        * Information on the number of messages handled & heartbeat of queue polling (Trace level). You can use this to confirm you're receiving messages. Beware, it can get big!
* "EventLog"
        * A full log of all the messages you publish (including the Json serialised version).
        *

Here's a snippet of the expected configuration:

````xml
    <logger name="EventLog" minlevel="Trace" writeTo="logger-specfic-log" final="true" />
    <logger name="JustSaying" minlevel="Trace" writeTo="logger-specfic-log" final="true" />

      <target
         name="logger-specfic-log"
         xsi:type="File"
         fileName="${logdir}\${loggerspecificlogfilename}"
         layout="${standardlayout}"
         archiveFileName="${logdir}\${loggerspecificlogfilename}"
         archiveEvery="Hour"
         maxArchiveFiles="8784"
         concurrentWrites="true"
         keepFileOpen="false"
      />
````

## Dead letter Queue (Error queue)

JustSaying supports error queues and this option is enabled by default. When a handler is unable to handle a message, JustSaying will attempt to re-deliver the message up to 5 times (handler retry count is configurable) and if the handler is still unable to handle the message then the message will be moved to an error queue.
You can opt out during subscription configuration.

## IAM Requirements

JustSaying requires the following IAM actions to run smoothly;

```text
// SNS
sns:CreateTopic
sns:ListTopics
sns:SetSubscriptionAttributes
sns:Subscribe

// SQS
sqs:ChangeMessageVisibility
sqs:CreateQueue
sqs:DeleteMessage
sqs:GetQueueAttributes
sqs:GetQueueUrl
sqs:ListQueues
sqs:ReceiveMessage
sqs:SetQueueAttributes
```

An example policy would look like;

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
                "sqs:SetQueueAttributes"
            ],
            "Resource": "arn:aws:sqs:aws-region:aws-account-id:uk-myfeature-orderaccepted"
        },
        {
            "Effect": "Allow",
            "Action": [
                "sns:CreateTopic",
                "sns:Publish",
                "sns:Subscribe"
            ],
            "Resource": "arn:aws:sqs:aws-region:aws-account-id:uk-orderaccepted"
        }
    ]
}
```

## Message formats and AWS interactions

JustSaying uses a number of Amazon Web Services APIs to transport messages between publishers and subscribers. Since these HTTP APIs can be used by applications which do not use JustSaying, it is entirely possible to have, for example, a Java application publishing a message which is subscribed to by an application using JustSaying. Equally, a C# application publishing messages using JustSaying, which are subscribed to by a Java application, is fully supported by AWS. In order to support this interoperability, it is important that the actual message formats are described. Since Amazon provide SDKs for many programming languages and frameworks, you are unlikely to interact with the APIs purely via HTTP, but knowing the structure of the JustSaying message JSON itself is useful for cross-language purposes.

### Publishing to SNS

JustSaying uses the SNS Publish API to send messages to SNS (documented [here](https://docs.aws.amazon.com/sns/latest/api/API_Publish.html)). The following parameters are set:

#### TopicArn
The ARN of the topic to which the message is being published.

#### Subject
JustSaying will set this to the type of the .NET message class - "OrderAccepted", for example.

#### Message
The actual message object, serialised to JSON. Because JustSaying messages must derive from the base Message class, the properties on that base class will be serialised into this JSON. On the base class, the `Id` property is set as a random GUID and the `TimeStamp` property is set to the current time (in UTC). The default serialisation settings will ignore null properties, so properties like `Conversation` will not appear in the JSON if they are left as the default null value. Enums are serialised as strings.

In summary, suppose we have the following message class:

````c#
        public class OrderAccepted : Message
        {
            public OrderAccepted(int orderId)
            {
                OrderId = orderId;
            }
            public int OrderId { get; private set; }
        }
````

And suppose I create a message object from that class:

````c#
        var orderAccepted = new OrderAccepted(1234)
        {
            RaisingComponent = "my publisher",
        };
````

This will result in the following JSON being created and populated on that Message parameter to the SNS Publish API:

````json
{
    "OrderId": 1234,
    "Id": "e3f84a55-b677-43df-8c24-92c170fdd89f",
    "TimeStamp": "2019-07-03T09:53:09.2956149Z",
    "RaisingComponent": "my sns publisher"
}
````

#### MessageAttributes
Any message attributes that you have chosen to send along with the message.

### Sending to SQS ("point to point")

JustSaying uses the SQS SendMessage API to send messages to SQS (documented [here](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessage.html)). The following parameters are set:

#### QueueUrl
The URL of the queue to which the message is being sent.

#### DelaySeconds
JustSaying will set this value to be that of the `Delay` property on the `PublishMetadata` class which is supplied as a parameter to the `PublishAsync` method.

#### MessageBody
Aa JSON object containing two properties, `Subject` and `Message`. `Subject`, as with the SNS publisher, will be the type of the .NET message class. `Message` will be the actual message object, serialised to JSON. The same serialisation applies as for the SNS publisher.

As an example, if I create the same OrderAccepted object as before and send that to SQS, then the value of this parameter will be:

````json
{
    "Subject":"OrderAccepted",
    "Message":"{\"OrderId\":1234,\"Id\":\"e3f84a55-b677-43df-8c24-92c170fdd89f\",\"TimeStamp\":\"2019-07-03T09:53:09.2956149Z\",\"RaisingComponent\":\"my sqs publisher\"}"
}
````

This structure is to ensure that messages that are delivered to SQS queues as a result of directly publishing to the queue have the same format as messages delivered to a queue as a result of being subscribed to an SNS topic to which a message was published. So, from the point of view of a subscriber, whether the message was originally delivered to SNS or SQS is not important.

### Subscribing to SQS

Whether the message was originally published to an SNS topic or sent directly to an SQS queue, the subscriber will receive it by reading from an SQS queue. To do this, JustSaying uses the SQS ReceiveMessage API to receive messages from SQS (documented [here](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_ReceiveMessage.html)). The following parameters are set in JustSaying's request to this API:

#### QueueUrl
The URL of the queue from which messages are to be received.

#### MaxNumberOfMessages
JustSaying will vary this value depending on the number of CPU cores available and throttling configuration. 

#### WaitTimeSeconds
JustSaying sets this to 20.

#### AttributeNames
JustSaying will only set ApproximateReceiveCount for this parameter.

The response from the API is an object with a single `Messages` property (documented [here](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_Message.html)). The JSON format of each message therein is described [here](https://docs.aws.amazon.com/sns/latest/dg/sns-sqs-as-subscriber.html) and [here](https://docs.aws.amazon.com/sns/latest/dg/sns-message-and-json-formats.html#http-notification-json).

As an example, the following response would be available for that same sample message if it had been published to an SNS topic and then received from an SQS queue subscribed to that topic:

````json
{
  "ReceiveMessageResponse": {
    "ReceiveMessageResult": {
      "messages": [
        {
          "Attributes": null,
          "Body": "{\n  \"Type\" : \"Notification\",\n  \"MessageId\" : \"redacted\",\n  \"TopicArn\" : \"arn:aws:sns:eu-west-1:redacted:mytopic\",\n  \"Subject\" : \"OrderAccepted\",\n  \"Message\" : \"{\\n    \\\"OrderId\\\": 1234,\\n    \\\"Id\\\": \\\"e3f84a55-b677-43df-8c24-92c170fdd89f\\\",\\n    \\\"TimeStamp\\\": \\\"2019-07-03T09:53:09.2956149Z\\\",\\n    \\\"RaisingComponent\\\": \\\"my sns publisher\\\"\\n}\\n\",\n  \"Timestamp\" : \"2019-07-10T12:38:58.830Z\",\n  \"SignatureVersion\" : \"1\",\n  \"Signature\" : \"redacted\",\n  \"SigningCertURL\" : \"redacted\",\n  \"UnsubscribeURL\" : \"redacted\"\n}",
          "MD5OfBody": "redacted",
          "MD5OfMessageAttributes": null,
          "MessageAttributes": null,
          "MessageId": "redacted",
          "ReceiptHandle": "redacted"
        }
      ]
    },
    "ResponseMetadata": {
      "RequestId": "redacted"
    }
  }
}
```` 

## Power tool

JustSaying comes with a power tool console app that helps you manage your SQS queues from the command line.
At this point, the power tool is only able to move an arbitrary number of messages from one queue to another.

````text
JustSaying.Tools.exe move -from "source_queue_name" -to "destination_queue_name" -in "region" -count "1"
````

## Sample Application

- To run the sample application against a simulated AWS SQS / SNS endpoint run this container
    ```sh
    docker pull pafortin/goaws
    docker run -d --name goaws -p 4100:4100 pafortin/goaws
    ```
- Alternatively to use your real AWS account
    - Locate the setup code `services.AddJustSaying(...)` in both [`Program.cs`](./samples/JustSaying.Sample.Restaurant.KitchenConsole/Program.cs) and [`Startup.cs`](./samples/JustSaying.Sample.Restaurant.JustSaying.Sample.Restaurant.OrderingApi/Startup.cs)
    - Remove the references to the 'ServiceUrl`
    - Add references to `x.WithCredentials(...)` supplying your real aws credentials
- Execute both `KitchenConsole` and `OrderingApi` applications

- Demonstrates
  - Publishing messages to a SNS Topic from a WebApi application and Console Application
  - Receiving messages from a SQS queue subscribed to a SNS topic in a WebApi application and Console Application

- Further samples in progress
  - Demonstrate use of `PublishMetaData`
  - Demonstrate integration with [CorrelationId](https://www.nuget.org/packages/CorrelationId/)
  - Demonstrate use of Cancellation Tokens
  - Add acceptance tests project
  - Add dockerfile for api, console, aws and tests
  - Demonstrate docker-compose executing acceptance tests against other images
  
## Contributing...

Please read the [contributing guide](./.github/CONTRIBUTING.md "Contributing to JustSaying").

### The End.....

...*Happy Messaging!...*
