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
