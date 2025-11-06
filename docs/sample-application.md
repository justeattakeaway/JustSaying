# Sample Application

[Sample Application found here](https://github.com/justeat/JustSaying/tree/master/samples/src)

* To run the sample application against a simulated AWS SQS / SNS endpoint run this container

  ```bash
    docker pull pafortin/goaws
    docker run -d --name goaws -p 4100:4100 pafortin/goaws
  ```

* Alternatively to use your real AWS account
  * Locate the setup code `services.AddJustSaying(...)` in both [`Program.cs`](./samples/JustSaying.Sample.Restaurant.KitchenConsole/Program.cs) and [`Startup.cs`](./samples/JustSaying.Sample.Restaurant.JustSaying.Sample.Restaurant.OrderingApi/Startup.cs)
  * Remove the references to the 'ServiceUrl\`
  * Add references to `x.WithCredentials(...)` supplying your real aws credentials
* Execute both `KitchenConsole` and `OrderingApi` applications
* Demonstrates
  * Publishing messages to a SNS Topic from a WebApi application and Console Application
  * Receiving messages from a SQS queue subscribed to a SNS topic in a WebApi application and Console Application
* Further samples in progress
  * Demonstrate use of `PublishMetaData`
  * Demonstrate integration with [CorrelationId](https://www.nuget.org/packages/CorrelationId/)
  * Demonstrate use of Cancellation Tokens
  * Add acceptance tests project
  * Add dockerfile for api, console, aws and tests
  * Demonstrate docker-compose executing acceptance tests against other images

