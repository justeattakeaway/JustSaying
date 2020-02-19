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
            public int OrderId { get; set; }
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
        publisher.Publish(new OrderAccepted {OrderId = 123456});
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
sns:Publish
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

JustSaying uses a number of Amazon Web Services APIs to transport messages between publishers and subscribers. Since these HTTP APIs can be used by applications which do not use JustSaying, it is entirely possible to have, for example, a Java application publishing a message which is subscribed to by an application using JustSaying. Equally, a C# application publishing messages using JustSaying, which are subscribed to by a Java application, is fully supported by AWS.

In order to support this interoperability, it is important that the actual message formats are described. Since Amazon provide SDKs for many programming languages and frameworks, you are unlikely to interact with the APIs purely via HTTP, but knowing the structure of the JustSaying message JSON itself is useful for cross-language purposes.

### Publishing to SNS

JustSaying uses the SNS Publish API to send messages to SNS (documented [here](https://docs.aws.amazon.com/sns/latest/api/API_Publish.html)). The following parameters are set:

#### TopicArn
The ARN of the topic to which the message is being published.

#### Subject
JustSaying will set this to the type name of the .NET message class - "OrderAccepted", for example.

Since a JustSaying _subscriber_ relies on this string to know what type to deserialise the JSON in the Message parameter into, non-JustSaying _publishers_ should restrict this parameter to alphanumeric characters.

#### Message
The actual message object, serialised to JSON. It is worth noting that the JSON sent in this parameter becomes a subset of the JSON that will be delivered (by the SNS service) to an SQS subscriber. This is described [here](https://docs.aws.amazon.com/sns/latest/dg/sns-sqs-as-subscriber.html) and [here](https://docs.aws.amazon.com/sns/latest/dg/sns-message-and-json-formats.html#http-notification-json).

In addition to those properties you will define in your own messages, the following are available to a JustSaying publisher:

|Property|Type|Description|Populated by default?|
|--------|----|-----------|:-------:|
|Id|GUID|A random GUID which uniquely identifies the message.|Yes|
|TimeStamp|DateTime|The current time, in UTC, in [ISO 8601 format](https://en.wikipedia.org/wiki/ISO_8601).|Yes|
|RaisingComponent|string|A string which identifies the publishing application - "orderapi", for example.|No|
|Version|string|The message version - "2", for example.|No|
|SourceIp|string|The IP address of the machine which published the message.|No|
|Tenant|string|In a multi-tenant architecture, a string to identify the tenant for which the message is applicable.|No|
|Conversation|string|A string which can be used to correlate multiple messages (to identify messages belonging to a single operation/customer journey, for example).|No|

Properties which are not populated will not appear in the message JSON. Enumerations are serialised as strings.

In summary, suppose we have the following message class:
````c#
        public class OrderAccepted : Message
        {
            public int OrderId { get; set; }
        }
````

And suppose I create a message object from that class:

````c#
        var orderAccepted = new OrderAccepted
        {
            OrderId = 1234,
            RaisingComponent = "orderapi",
        };
````

This will result in the following JSON being created and populated on that Message parameter to the SNS Publish API:

````json
{
    "OrderId": 1234,
    "Id": "e3f84a55-b677-43df-8c24-92c170fdd89f",
    "TimeStamp": "2019-07-03T09:53:09.2956149Z",
    "RaisingComponent": "orderapi"
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
A JSON object containing two properties, `Subject` and `Message`. `Subject`, as with the SNS publisher, will be the type name of the .NET message class. `Message` will be the actual message object, serialised to JSON. The same serialisation applies as for the SNS publisher.

As an example, if I create the same OrderAccepted object as before and send that to SQS, then the value of this parameter will be:

````json
{
    "Subject": "OrderAccepted",
    "Message": "{\"OrderId\":1234,\"Id\":\"e3f84a55-b677-43df-8c24-92c170fdd89f\",\"TimeStamp\":\"2019-07-03T09:53:09.2956149Z\",\"RaisingComponent\":\"orderapi\"}"
}
````

This structure is to ensure that messages that are delivered to SQS queues as a result of directly publishing to the queue have the same format as messages delivered to a queue as a result of being subscribed to an SNS topic to which a message was published. So, from the point of view of a subscriber, whether the message was originally delivered to SNS or SQS is not important.

### Subscribing to SQS

Whether the message was originally published to an SNS topic or sent directly to an SQS queue, the subscriber will receive it by reading from an SQS queue. To do this, JustSaying uses the SQS ReceiveMessage API to receive messages from SQS (documented [here](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_ReceiveMessage.html)). The following parameters are set in JustSaying's request to this API:

#### QueueUrl
The URL of the queue from which messages are to be received.

#### MaxNumberOfMessages
JustSaying will vary this value depending on the number of CPU cores available and throttling configuration (via the `Throttled` class).

#### WaitTimeSeconds
JustSaying sets this to 20.

#### AttributeNames
JustSaying will set ApproximateReceiveCount for this parameter.

The response from the API is an object with a single `Messages` property (documented [here](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_Message.html)). The JSON format of each message therein is described [here](https://docs.aws.amazon.com/sns/latest/dg/sns-sqs-as-subscriber.html) and [here](https://docs.aws.amazon.com/sns/latest/dg/sns-message-and-json-formats.html#http-notification-json).

As an example, the following response would be available for that same sample message if it had been published to an SNS topic and then received from an SQS queue subscribed to that topic:

````json
{
  "ReceiveMessageResponse": {
    "ReceiveMessageResult": {
      "messages": [
        {
          "Attributes": {
              "ApproximateReceiveCount": "1"
          },
          "Body": "{\n  \"Type\" : \"Notification\",\n  \"MessageId\" : \"3b4d4581-9a41-5a29-82bb-101e44e3944d\",\n  \"TopicArn\" : \"arn:aws:sns:eu-west-1:12345678:orderaccepted\",\n  \"Subject\" : \"OrderAccepted\",\n  \"Message\" : \"{\\n    \\\"OrderId\\\": 1234,\\n    \\\"Id\\\": \\\"e3f84a55-b677-43df-8c24-92c170fdd89f\\\",\\n    \\\"TimeStamp\\\": \\\"2019-07-03T09:53:09.2956149Z\\\",\\n    \\\"RaisingComponent\\\": \\\"orderapi\\\"\\n}\\n\",\n  \"Timestamp\" : \"2019-07-10T12:38:58.830Z\",\n  \"SignatureVersion\" : \"1\",\n  \"Signature\" : \"oKkS6tDLhiBDiIQEK1ez2XCETboVWJlQfLSinS0tPCyjOWGDLl9w8l1AUNxwT4deF7p3jZ1jGUWz/dRpcfB/koUAbSKwQgcrlJUO2IFdlMVMOi/xfpw99akaImMIFGq7lxmviIkiqNBfmTosYgnE1XWvn8sUupjJSwIg063St+chIfrx4DNcwKIKCGz7suV3+TKSXpBHrTZgq9hHqk9MpYjahgLl9rU1jXX54ABjE3rHyapW5TmQEbkE745kmPXrxE966Q+S+2/W10PzJdEbpEQ8eSOHebnMRK/DSHyBXUeDUz75NpU+eKf0zQ/ATSdE0Xt/kv7SCPRecbcSmMEIew==\",\n  \"SigningCertURL\" : \"https://sns.eu-west-1.amazonaws.com/SimpleNotificationService-6aad65c2f9911b05cd53efda11f913f9.pem\",\n  \"UnsubscribeURL\" : \"https://sns.eu-west-1.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:eu-west-1:12345678:orderaccepted:f02348fc-5473-4cf7-8873-dfb2acc3029a\"\n}",
          "MD5OfBody": "173f1033094262aad820ff4ea0da84e9",
          "MD5OfMessageAttributes": null,
          "MessageAttributes": null,
          "MessageId": "6fc2735b-30ac-4a75-b4b4-e62ccee39c3b",
          "ReceiptHandle": "AQEBZCkMIWhh3xUmP8Rt5FOPRaymFLOECo1qWmG9czPzBcqppUZQ+3k0p6RWkGnKvMz9M0rU5319N6O9TNO8yo1evbbXwyxDegARHJ/E+0wNG0e5cr0OThR/3ZDC1hMR3NQ+xxu/n7AbTzmnBydghJjQrlXS2luyisKYMtZCCtWjn9u25sNHO6mlMOD2g/qleXmRF49ciNIVIkxV+C0c5/K2mJQITacb7YwHspuo41ZYPAXTyFIc6Ycso6Dtrrpd15Cj9JMulhzdech9xI6kfwFzVlmkI4hqsB0ZUJch8Bp87nf36OGsn+3Ev2HswsVcpmQruAl+U7Ar5kJERy/zeeTW1xPdOrSFJWSodVy1fgnKYnEVGlrwZq8Cn0WQSSVOaM1bayjExjfYlf4j1BkpTOZ+2w=="
        }
      ]
    },
    "ResponseMetadata": {
      "RequestId": "e43c0585-fda5-5948-aa72-501a230c37a4"
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
