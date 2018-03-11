# JustSaying 5.0.0 Release Notes
This release has been a long time in the making, I want to say a huge thank you to everyone that contributed, and to everyone that was patient in waiting for this release.

The big changes in this release are:

### Re-implemented on top of AWSSDK v3
In version 3 of the AWSSDK, Amazon changed the package names, and split them into smaller individual packages per service. They also removed synchronous calls for target frameworks other than the full .NET framework.

### .NET Standard 1.6 Support
This means you can now consume this from any supporting platforms, including .NET Core 1.0 and up. We continue to target `net451`, so no one is left behind in this release.

### Project and NuGet Package re-rationalization
In v4 there were 4 assemblies and one NuGet package. Most of these assemblies weren't logically decoupled and didn't really add anything. In the newer .NET project system, having multiple assemblies per package is fighting against the tools.

We now have 2 pakcages:
`JustSaying`
`JustSaying.Models`

Each containing just 1 assembly.
`JustSaying.Models` actually already exists, at version `2.0.0.x`, but then disappeared from the solution after.
If you are using `JustSaying` v4 and any version of `JustSaying.Models`, then you could run into runtime issues, you will likely need to upgrade.

### More async
We now have a `PublishAsync` method for example. See known issues for where we are not yet asynchronous.

### Logging abstracted
In v4 of `JustSaying` and below, `NLog` was a hard dependency, this wasn't very friendly to people that don't use `NLog`, so we have abstracted this.
We settled on using `Microsoft.Extensions.Logging.Abstractions` rathen than reimplementing the wheel.
There are [many providers](https://github.com/aspnet/Logging#providers) today you can use, including `NLog`, `Serilog` and Microsoft provided ones such as Console logger etc...

Logging is a mandatory requirement for using `JustSaying`, if you want to turn off logging you should start your fluent configuration with:
```csharp
.WithLogging(new LoggerFactory())
```

### Message handling backoff strategies
We have added the ability to introduce a backoff strategy for message handling retrying.
There is a new interface `IMessageBackoffStrategy`, which looks like this:
```csharp
public interface IMessageBackoffStrategy
{
    TimeSpan GetBackoffDuration(Message message, int approximateReceiveCount, Exception lastException = null);
}
```

You simply return the `TimeSpan` you want to wait for, and you can use the message, receive count, or last exception as factors for your backoff duration. Whatever your returned `TimeSpan`, JustSaying will set the message [Visibility Timeout](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-visibility-timeout.html), which can be between 0 and 43200 seconds (12 hours), this ensures nothing else will attempt to dequeue the message for this duration.

Just pass the implementation during the subscription configuration like like:
```csharp
var bus = CreateMeABus
            .WithLogging(loggerFactory)
            .InRegion(RegionEndpoint.EUWest1.SystemName)
            .WithMonitoring(_monitoring)
            .WithSqsTopicSubscriber()
            .IntoQueue("queuename")
            .ConfigureSubscriptionWith(cfg =>
                {
                    cfg.MessageBackoffStrategy = myFancyBackoffStrategy // <- NEW
                })
            .WithMessageHandler(_handler);
```

### Ability to handle publish exceptions per publisher
You can now configure publishers and set the `HandleException` property with a delegate that can deal with the exception, and a boolean indicating if it has been handled or if it wants the exception to propagate.

### Embedded source and embedded PDBs
The assembies now have the PDBs embedded, and to make debugging completely dependency free, the PDBs have the source embedded. Hurray!

### All the fixes cherry-picked from v4
As this was such a big change, v5 was developed alongside v4, which contained some smaller (but important) changes, if you are upgrading from 4.0 then you will get the benefit from these too:


### Discovering Topics more efficiently
When checking to see if an SNS Topic already exists, we were using the `FindTopic` AWSSDK method, which you might expect to make a single API call to return a single SNS Topic. Unfortunately it doesn't do this, within this method it calls `ListTopics`, then it picks out the topic in memory. For AWS accounts with a large number of topics, this became very slow.

We now don't make this check, as creating a topic is idempotent, this results in a rapid startup time for users under the above conditions.

## Upgrade instructions

Let's take the following snippet that uses JustSaying v4, and let's update it to v5:
```csharp
var bus = CreateMeABus
            .InRegion(RegionEndpoint.EUWest1.SystemName)
            .WithMonitoring(_monitoring)
            .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoffMilliseconds = 1;
                    c.PublishFailureReAttempts = 3;
                })
            .WithSnsMessagePublisher<GenericMessage>()
            .WithSqsTopicSubscriber()
            .IntoQueue("queuename")
            .ConfigureSubscriptionWith(cfg =>
                {
                    cfg.MessageRetentionSeconds = 60;
                    cfg.InstancePosition = 1;
                    cfg.OnError = _globalErrorHandler;
                })
            .WithMessageHandler(_handler);
```

After you update the JustSaying pacakge, be sure to tidy up any `app.config` assembly binding redirects that mention `JustSaying.AwsTools` and `JustSaying.Messaging` as these no longer exist, NuGet should do this for you.

The snippet above won't compile as we need to configure a logger before doing any further configuration:
```csharp
// You might use a LoggerFactory throughout your application,
//   or you might be using your logging library abstraction, and this is just a bridge
var loggerFactory = new LoggerFactory();
loggerFactory.AddMyFavouriteLoggingLibrary(); // Example: .AddSerilog()

var bus = CreateMeABus
            .WithLogging(loggerFactory) // <- NEW
            .InRegion(RegionEndpoint.EUWest1.SystemName)
            ...
```

### IAM permissions
In this release the AWS APIs that are called have changed slightly with regards to SNS, so it might be worth checking that you have the required IAM permissions set up.

Here are the actions requred if you are both publishing and subscribing.
```
sns:CreateTopic,
sns:ListSubscriptionsByTopic
sns:ListTopics
sns:Subscribe
sqs:CreateQueue
sqs:DeleteMessage
sqs:GetQueueAttributes
sqs:GetQueueUrl
sqs:ListQueues
sqs:SetQueueAttributes
sqs:ReceiveMessage
```

If you are just publishing then you can omit the SQS actions. If you are just subscribing then you still need the SNS actions (may change in the future).

## Known issues

### Synchronous initialization.
The fluent configuration is not lazy, and does asynchronous work (on non-full .NET AWSSDK is async only), but does so in a blocking way. The configuration is designed to be run once in your application in a context where blocking and thread usage should not be an issue.

We are aware that this isn't ideal, and have a branch where we are fixing it by adding an awaitable `.BuildAsync()` method, this is a reasonable breaking change so we want to take the opportunity to refactor and rationalise the fluent API design which will likely come in v6.

### Logging required up-front
Now that we have added configurable logging, and because the fluent APIs are not lazy, the first thing you have to provide us is a `loggerFactory`. Again this will likely change in v6, where you would configure it at any point before awaiting `.BuildAsync()`.

### `IHandlerResover` changes
The `Resolve` method on `IHandlerResolver` previously used to be invoked a few times during initialization, then the returned `IHandlerAsync`  instance was cached as singltons per subscription. This was counter intuative, and was the cause of some confusion. We have simplified the behaviour and now `Resolve` will be called once per message. If you rely on this behaviour within `JustSaying` to give you the effect of singletons, you might need to change your resolver to ensure it is explicity shared across invocations.

### Lots of dependencies when consuming from full .NET
We were careful to hand-pick the packages we depend on, rather than include just `NETStandard.Library`. However because we now depend on `Microsoft.Extensions.Logging.Abstractions`, version `1.1.2` of this will pull in `NETStandard.Library 1.6.1`, which will bring in "the whole world". This might be fixed by additionally targeting `.NET Standard 2.0`, which we may try for JustSaying 5.1

Contributions and thanks to:
- @AnthonlySteele
- @pierskarsenbarg
- @adammorr
- @slang25
- @andrewchaa
- @JosephWoodward
- @shaynevanasperen
- @JonahAcquah
- @brainmurphy
- @martincostello
- @Liewe
- Tony Harverson
- Mark England
... hope I haven't missed anyone
