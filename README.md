#JustEat.Simples.NotificationStack

A helpful library for publishing and consuming events / messages over SNS from Just-Eat applications.

##Publishing Messages
Here's how to get up & running with message publishing.

###1. Create a message object

* These can be as complex as you like (provided it is under 256k serialised as Json).
* They must be derived from the abstract Message class.

        public class OrderAccepted : Message
        {
            public OrderAccepted(int orderId)
            {
                OrderId = orderId;
            }
            public int OrderId { get; private set; }
        }

###2. Registering publishers
* You will need to tell FluentNotificationStack where to publish your messages to.
* In this case, we are telling it to publish the OrderAccepted message to the 'OrderProcessing' topic.

          var publisher = FluentNotificationStack.Register(configuration => {
                    configuration.Component = Component.OrderValidator;  
                    configuration.Environment = "qa12";  
                    configuration.Tenant = "uk";  
                    configuration.PublishFailureReAttempts = 3;  
                    configuration.PublishFailureBackoffMilliseconds = 100;  
            })  
            .WithSnsMessagePublisher<OrderAccepted>("OrderProcessing");  

###3. Publish a message

* This can be done wherever you want within your application.
* Simply pass the publisher object through using your IOC container.
* In this case, we are publishing the fact that a given order has beenAccepted.

        publisher.Publish(new OrderAccepted(7349753));

BOOM! You're don publishing!

## Consuming messages
Here's how to get up & running with message comsumption.
We currently support SQS subscriptions only, but keep checking back for other methods too (although we are kinda at the mercy of AWS here for HTTP...)


###1. Create Handlers
* We tell the stack to handle messages by implementing a generic interface which contains our message type
* Here, we're creating a handler for OrderAccepted messages.
* This is where you pass on to your BLL layer.
* We also need to tell the stack whether we handled the message as expected. We can say things got messy either by returning false, or bubbling up exceptions.

        public class CustomerNotificationHandler : IHandler<OrderAccepted>
        {
            public bool Handle(OrderAccepted message)
            {
                bll.SendCustomerEmail(message.OrderId);
                return true;
            }
        }

###2. Register a subscription
* This can be done at the same time as your publications are set out.
* There is no limit to the number of handlers you add to a subscription.
* You can specify message retention policies etc in your subscription for resiliency purposes.
* In this case, we are telling the stack to keep 'OrderDispatch' topic messages for one minute. They will be thrown away if not handled in this time.
* We are telling it to keep 'OrderProcessing' topic messages for 2 mins, and not to handle them again on failure for 30 seconds


            FluentNotificationStack.Register(configuration => { })
            .WithSqsTopicSubscriber(Topic.OrderDispatch, 60)
                .WithMessageHandler<OrderAccepted>(new CustomerNotificationHandler())
                .WithMessageHandler<OrderRejected>(new CustomerNotificationHandler())
            .WithSqsTopicSubscriber(Topic.OrderProcessing, 120, 30)
                .WithMessageHandler(new TellGuardAboutFailedOrderHandler())
                .StartListening();
That's it. By calling StartListening() we are telling the stack to begin polling SQS for incoming messages.

##Contributing...
We've been adding things ONLY as they are needed, so please feel free to either bring up suggestions or to submit pull requests with new *GENERIC* functionalities.

Don't bother submitting any breaking changes or anything without unit tests against it. It will be declined.

###The End.....
...*Happy Messaging!...*

AJ