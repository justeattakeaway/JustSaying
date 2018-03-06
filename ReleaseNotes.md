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

TODO
Show logging
Show packages changes with csproj snippet
Show IAM changes
Show backoff strategy stuff

## Known issues

### Synchronous initialization.
The fluent configuration is not lazy, and does asynchronous work (on non-full .NET AWSSDK is async only), but does so in a blocking way. The configuration is designed to be run once in your application in a context where blocking and thread usage should not be an issue.

We are aware that this isn't ideal, and have a branch where we are fixing it by adding an awaitable `.BuildAsync()` method, this is a reasonable breaking change so we want to take the opportunity to refactor and rationalise the fluent API design which will likely come in v6.

### Logging required up-front
Now that we have added logging as something you can plug into, because the fluent APIs are not lazy, the first thing you have to provide us is logging. Again this will likely change in v6, where you can configure it at any point before awaiting `.BuildAsync()`.

### `IHandlerResover` changes

### Lots of dependencies when consuming from full .NET
We were careful to hand-pick the packages we depend on, rather than include just `NETStandard.Library`. However because we now depend on `Microsoft.Extensions.Logging.Abstractions`, version `1.1.2` of this will pull in `NETStandard.Library 1.6.1`, which will bring in "the whole world". This might be fixed by additionally targeting `.NET Standard 2.0`, which we may try for JustSaying 5.1

Contributions and thanks to:
@AnthonlySteele
@pierskarsenbarg
@adammorr
@andrewchaa
@JosephWoodward
@shaynevanasperen
@JonahAcquah
@brainmurphy
@martincostello
Tony Harverson
Mark England
... hope I haven't missed anyone
