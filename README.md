#JustSaying

A helpful library for publishing and consuming events / messages over SNS from Just-Eat applications.

##Publishing Messages
Here's how to get up & running with message publishing.

###1. Create a message object

* These can be as complex as you like (provided it is under 256k serialised as Json).
* They must be derived from the abstract Message class.

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
* You will need to tell FluentNotificationStack where to publish your messages to.
* In this case, we are telling it to publish the OrderAccepted message to the 'OrderProcessing' topic.

````c#
          var publisher = FluentNotificationStack.Register(configuration => {
                    configuration.Component = Component.OrderValidator;  
                    configuration.Environment = "qa12";  
                    configuration.Tenant = "uk";  
                    configuration.PublishFailureReAttempts = 3;  
                    configuration.PublishFailureBackoffMilliseconds = 100;  
            })  
            .WithSnsMessagePublisher<OrderAccepted>("OrderProcessing");  
````

###3. Publish a message

* This can be done wherever you want within your application.
* Simply pass the publisher object through using your IOC container.
* In this case, we are publishing the fact that a given order has beenAccepted.

````c#
        publisher.Publish(new OrderAccepted(7349753));
````

BOOM! You're don publishing!

## Consuming messages
Here's how to get up & running with message comsumption.
We currently support SQS subscriptions only, but keep checking back for other methods too (although we are kinda at the mercy of AWS here for HTTP...)


###1. Create Handlers
* We tell the stack to handle messages by implementing a generic interface which contains our message type
* Here, we're creating a handler for OrderAccepted messages.
* This is where you pass on to your BLL layer.
* We also need to tell the stack whether we handled the message as expected. We can say things got messy either by returning false, or bubbling up exceptions.

````c#
        public class CustomerNotificationHandler : IHandler<OrderAccepted>
        {
            public bool Handle(OrderAccepted message)
            {
                bll.SendCustomerEmail(message.OrderId);
                return true;
            }
        }
````

###2. Register a subscription
* This can be done at the same time as your publications are set out.
* There is no limit to the number of handlers you add to a subscription.
* You can specify message retention policies etc in your subscription for resiliency purposes.
* In this case, we are telling the stack to keep 'OrderDispatch' topic messages for one minute. They will be thrown away if not handled in this time.
* We are telling it to keep 'OrderProcessing' topic messages for 2 mins, and not to handle them again on failure for 30 seconds

````c#
            FluentNotificationStack.Register(configuration => {
                    configuration.Component = Component.OrderValidator;  
                    configuration.Environment = "qa12";  
                    configuration.Tenant = "uk";  
                    configuration.PublishFailureReAttempts = 3;  
                    configuration.PublishFailureBackoffMilliseconds = 100;  
            })  
            .WithSqsTopicSubscriber(Topic.OrderDispatch, 60)
                .WithMessageHandler<OrderAccepted>(new CustomerNotificationHandler())
                .WithMessageHandler<OrderRejected>(new CustomerNotificationHandler())
            .WithSqsTopicSubscriber(cf =>
                {
                    cf.Topic = Topic.OrderProcessing;
                    cf.MessageRetentionSeconds = 120;
                    cf.VisibilityTimeoutSeconds = NotificationStackConstants.DEFAULT_VISIBILITY_TIMEOUT;
                });
                .WithMessageHandler(new TellGuardAboutFailedOrderHandler())
                .StartListening();
````

That's it. By calling StartListening() we are telling the stack to begin polling SQS for incoming messages.

###3. Enabling Throttling
By default throttling is off which means NotificationStack will create as many threads as it needs to process messages as fast as it can. 
By enabling throttling you can limit the amount of messages passed to application (useful for web apps with TCP thread restrictions).
To enable throttling you need to specify optional parameter when setting SqsTopicSubcriber

````c#

            .WithSqsTopicSubscriber(Topic.OrderDispatch, 60, maxAllowedMessagesInFlight: 100)
                .WithMessageHandler<OrderAccepted>(new CustomerNotificationHandler())

````

## Logging

Notification stack will throw out the following named logs from NLog:
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

## Power tool

JustSaying comes with a power tool console app that helps you mange your SQS queues from the command line.
At this point, the power tool is only able to move arbitary number of messages from one queue to another.
````
JustSaying.Tools.exe move -from "source_queue_name" -to "destination_queue_name" -count "1"
````

## Ruby Testing (functional tests) snippet

Ok, so I'm not much of a ruby guy... But here's a snippet which should help you get started with publishing messages in functional tests for your app to respond to. This is an example from OrderEngine.

````ruby
    def SendSnsNotification(message_type, order_id, customer_id = 0)
        case message_type
            when "CustomerRejectionSmsFailed"
              then
                message = "{\"FailureReason\":1,\"FailureDetails\":\"Something went wrong y'all\",\"OrderId\":#{order_id},\"CustomerId\":#{customer_id},\"TelephoneNumber\":\"\",\"CommunicationActivity\":3,\"TimeStamp\":\"2013-07-04T12:32:11.5258032Z\",\"RaisingComponent\":0,\"Version\":null,\"SourceIp\":null}"
                subject = "CustomerOrderRejectionSmsFailed"
                topic_name = "#{CountryWorld.country}-#{CountryWorld.environment}-customercommunication"
            when "CustomerRejectionSmsSucceeded"
              then
                message = "{\"OrderId\":#{order_id},\"CustomerId\":#{customer_id},\"TelephoneNumber\":\"\",\"CommunicationActivity\":2,\"TimeStamp\":\"2013-07-04T12:32:11.5258032Z\",\"RaisingComponent\":0,\"Version\":null,\"SourceIp\":null}"
                subject = "CustomerOrderRejectionSms"
                topic_name = "#{CountryWorld.country}-#{CountryWorld.environment}-customercommunication"
            when "OrderRejected"
              then
                message = "{\"orderid\":#{order_id},\"customerid\":#{customer_id},\"restaurantid\":#{@rest_id},\"orderrejectreason\":2,\"timestamp\":\"2013-07-04t12:32:11.5258032z\",\"raisingcomponent\":0,\"version\":null,\"sourceip\":null}"
                subject = "OrderRejected"
                topic_name = "#{CountryWorld.country}-#{CountryWorld.environment}-orderdispatch"
            when "OrderAccepted"
              then
                message = "{\"orderid\":#{order_id},\"customerid\":#{customer_id},\"restaurantid\":#{@rest_id},\"timestamp\":\"2013-07-04t12:32:11.5258032z\",\"raisingcomponent\":0,\"version\":null,\"sourceip\":null}"
                subject = "OrderAccepted"
                topic_name = "#{CountryWorld.country}-#{CountryWorld.environment}-orderdispatch"
          end
        
      sns = AWS::SNS.new(
        :access_key_id => 'AKIAIVOWJVKJVBQJDOTQ',
        :secret_access_key => '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',
        :sns_endpoint => 'sns.eu-west-1.amazonaws.com')
    
      topic = sns.topics.find {|x| x.name.include? topic_name }
      topic.publish(message, :subject => subject)
    end
````

##Contributing...
We've been adding things ONLY as they are needed, so please feel free to either bring up suggestions or to submit pull requests with new *GENERIC* functionalities.

Don't bother submitting any breaking changes or anything without unit tests against it. It will be declined.

###The End.....
...*Happy Messaging!...*

AJ