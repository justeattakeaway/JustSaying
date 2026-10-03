---
---

# Native AOT

JustSaying can be used in applications published with [Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/) and trimming. Its own code is trim- and AOT-safe, but the default message serializer isn't: System.Text.Json's reflection-based serialization isn't available under Native AOT. You need to give it a source-generated `JsonSerializerContext` instead.

## Registering a source-generated context

List every message type you publish or handle in a `JsonSerializerContext`, plus the runtime types of any `object`-typed values in them:

```csharp
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(OrderPlaced))]
[JsonSerializable(typeof(OrderShipped))]
public sealed partial class MessagesJsonContext : JsonSerializerContext;
```

Register it with a copy of JustSaying's default options, so the AOT build reads and writes messages the same way as everything else:

```csharp
var options = new JsonSerializerOptions(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
{
    TypeInfoResolver = MessagesJsonContext.Default,
};

services.AddSingleton<IMessageBodySerializationFactory>(new SystemTextJsonSerializationFactory(options));

services.AddJustSaying(config => { /* ... */ });
```

:::warning
`UseStringEnumConverter = true` is required. JustSaying writes enums as strings by default, using a converter that needs dynamic code, so it isn't there under Native AOT. Without the setting, an AOT service writes enums as numbers, and **can't read the strings** that other services send: those messages fail to deserialize and end up in the error queue. JustSaying can't check this for you.
:::

## Startup validation

When reflection-based serialization is off, as it is under Native AOT, a message type that's missing from the context fails when the bus is built, with an `InvalidOperationException` naming the type. So does using the default registration with no context at all. You find out at startup, not at the first publish or receive.

## CloudEvents

The [CloudEvents](/cloudevents/) `data` payload is serialized with the app's own `IMessageBodySerializationFactory`, so the context you register above covers it too. Add the payload types (`OrderPlaced`), not `CloudEvent<OrderPlaced>`: the envelope is written by JustSaying.

## What isn't AOT-compatible

* **Newtonsoft.Json.** `NewtonsoftSerializationFactory` is annotated as requiring unreferenced and dynamic code, and produces trim and AOT warnings.
* **`JustSaying.AsyncApi`.** It writes documents with ByteBard.AsyncAPI.NET, which isn't annotated for trimming, so publishing a trimmed or AOT application that references it produces warnings (IL2104, IL3053), and the package isn't marked as AOT-compatible.

## Testing AOT behaviour without publishing

To get AOT's serializer behaviour in `dotnet run` and `dotnet test`, without publishing natively, set this in the project:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

This turns off reflection-based serialization and dynamic code, so a type missing from the context fails at startup and enums are written by your context's settings, as they would be in the native build.

Building with `-p:JsonSerializerIsReflectionEnabledByDefault=false` turns off only reflection. That shows a type missing from the context, but enums are still written as strings, because dynamic code is still available, so it won't catch a missing `UseStringEnumConverter`.
