#JustSaying [![NuGet Version](http://img.shields.io/nuget/v/JustSaying.svg?style=flat)](https://www.nuget.org/packages/JustSaying/) [![NuGet Downloads](http://img.shields.io/nuget/dt/JustSaying.svg?style=flat)](https://www.nuget.org/packages/JustSaying/)

[![Join the chat at https://gitter.im/justeat/JustSaying](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/justeat/JustSaying?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

A helpful library for publishing and consuming events / messages over SNS (SNS / SQS as a message bus).

##Publishing Messages
Here's how to get up & running with simple message publishing.

###1. Create a message object (POCO)

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

###2. Registering publishers
* You will need to tell JustSaying which messages you intend to publish in order that it can setup any missing topics for you.
* In this case, we are telling it to publish the OrderAccepted messages.
* The topic will be the message type.

````c#
          var publisher = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithSnsMessagePublisher<OrderAccepted>();
````


###2.(a) Configuring publishing options
* You can also specify some publishing options (such as how to handle failures) using a configuration object like so:

````c#
         CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .ConfigurePublisherWith(c => { c.PublishFailureReAttempts = 3; c.PublishFailureBackoffMilliseconds = 50; })
                .WithSnsMessagePublisher<OrderAccepted>(); 
````


###3. Publish a message

* This can be done wherever you want within your application.
* Simply pass the publisher object through using your IOC container.
* In this case, we are publishing the fact that a given order has been accepted.

````c#
        publisher.Publish(new OrderAccepted(123456));
````

BOOM! You're done publishing!

## Consuming messages
Here's how to get up & running with message consumption.
We currently support SQS subscriptions only, but keep checking back for other methods too (http, Kinesis)
(although we are kinda at the mercy of AWS here for internal HTTP delivery...)


###1. Create Handlers
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

###2. Register a subscription
* This can be done at the same time as your publications are set up.
* There is no limit to the number of handlers you add to a subscription.
* You can specify message retention policies etc in your subscription for resiliency purposes.
* In this case, we are telling JustSaying to keep 'OrderAccepted' messages for the default time, which is one minute. They will be thrown away if not handled in this time.
* We are telling it to keep 'OrderFailed' messages for 1 minute and not to handle them again on failure for 30 seconds. These are the default values.

````c#
            CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithSqsTopicSubscriber()
                .IntoQueue("CustomerOrders")
                .WithMessageHandler<OrderAccepted>(new OrderNotifier())
                .StartListening();
````

That's it. By calling StartListening() we are telling the stack to begin polling SQS for incoming messages.


###2.(a) Subscription Configuration
* In this case, we are telling JustSaying to keep 'OrderAccepted' messages for the default time, which is one minute. They will be thrown away if not handled in this time.
* We are telling it to keep 'OrderFailed' messages for 5 mins, and not to handle them again on failure for 60 seconds

````c#
            CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithSqsTopicSubscriber()
                .IntoQueue("CustomerOrders")
                    .ConfigureSubscriptionWith(c => { c.MessageRetentionSeconds = 60; })
                        .WithMessageHandler<OrderAccepted>(new NotifyCustomerOfAcceptedOrder())
                    .ConfigureSubscriptionWith(c => { c.MessageRetentionSeconds = 300; c.VisibilityTimeoutSeconds = 60; })
                        .WithMessageHandler<OrderFailed>(new NotifyCustomerOfFailedOrder())
                .StartListening();
````


###2.(b) Enabling Throttling
By default throttling is off which means NotificationStack will create as many threads as it needs to process messages as fast as it can. 
By enabling throttling you can limit the amount of messages passed to application (useful for web apps with TCP thread restrictions).
To enable throttling you need to specify optional parameter when setting SqsTopicSubcriber

````c#

            .ConfigureSubscriptionWith(c => { c.MaxAllowedMessagesInFlight = 100; })
                .WithMessageHandler<OrderAccepted>(new NotifyCustomerOfAcceptedOrder())

````

###2.(c) Control Handlers' life cycle
You can tell JustSaying to delegate the creation of your handlers to an IoC container. All you need to do is to implement IHandlerResolver interface and pass it along when registering your handlers.
````c#
CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithSqsTopicSubscriber()
                .IntoQueue("CustomerOrders")
		.WithMessageHandler<OrderAccepted>(new HandlerResolver())
````

## Logging

JustSaying stack will throw out the following named logs from NLog:
* "JustSaying"
        * Information on the setup & your configuration (Info level). This includes all subscriptions, tennants, publication registrations etc.
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

JustSaying supports error queues and this option is enabled by default. When a handler is unable to handle a message, JustSaying will attempt to re-deliver the message up to 5 times (Handler retry count is configurable) and if the handler is still unable to handle the message then the message will be moved to an error queue. 
You can opt out during subscription configuration.

## Power tool

JustSaying comes with a power tool console app that helps you mange your SQS queues from the command line.
At this point, the power tool is only able to move arbitary number of messages from one queue to another.
````
JustSaying.Tools.exe move -from "source_queue_name" -to "destination_queue_name" -count "1"
````

##Contributing...
We've been adding things ONLY as they are needed, so please feel free to either bring up suggestions or to submit pull requests with new *GENERIC* functionalities.

Don't bother submitting any breaking changes or anything without unit tests against it. It will be declined.

###The End.....
...*Happy Messaging!...*

AJ
