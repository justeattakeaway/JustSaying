---
---

# AsyncAPI

[AsyncAPI](https://www.asyncapi.com/) is a format for describing message-driven APIs, in the way OpenAPI describes HTTP APIs. The `JustSaying.AsyncApi` package generates an AsyncAPI 3.1 document from your JustSaying configuration: the topics and queues the application publishes to and reads from, the messages on each, and a JSON schema for every message payload.

```bash
dotnet add package JustSaying.AsyncApi
```

The document can be generated while the application runs, as on this page, or [when it builds](build-time.md).

## Setup

Call `AddJustSayingAsyncApi` alongside `AddJustSaying`:

```csharp
builder.Services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("eu-west-1"));
    config.Publications(x => x.WithTopic<OrderPlaced>());
    config.Subscriptions(x => x.ForTopic<OrderShipped>());
});

builder.Services.AddJustSayingHandler<OrderShipped, OrderShippedHandler>();

builder.Services.AddJustSayingAsyncApi(options =>
{
    options.Title = "Orders";
    options.Description = "Events published and consumed by the orders service.";
});
```

## Generating the document

`AddJustSayingAsyncApi` registers an `IAsyncApiDocumentProvider`, which writes the document as JSON. It has one document, named `asyncapi`. For example, to serve it from a web application:

```csharp
app.MapGet("/asyncapi.json", async (IAsyncApiDocumentProvider provider, CancellationToken cancellationToken) =>
{
    using var writer = new StringWriter();
    await provider.GenerateAsync("asyncapi", writer, cancellationToken);
    return Results.Text(writer.ToString(), "application/json");
});
```

Generating the document builds the JustSaying bus, without starting it or calling AWS, to collect its publications and subscriptions. Building the bus resolves every subscription's handler, so all handlers must be registered, even in an application that only generates the document.

## Options

| Option | Description |
| --- | --- |
| `Title` | The document's title. Defaults to the host's application name. |
| `Version` | The version of the API. Defaults to `1.0.0`. |
| `Id`, `Description`, `TermsOfService` | Written to the document as they are. |
| `Contact`, `License`, `ExternalDocs` | `AsyncApiContactOptions` (`Name`, `Url`, `Email`), `AsyncApiLicenseOptions` (`Name`, `Url`) and `AsyncApiExternalDocsOptions` (`Description`, `Url`). |
| `Tags` | A list of `AsyncApiTagOptions` (`Name`, `Description`). |
| `Servers` | The servers the channels are available on, by name. See [below](#servers). |
| `SerializerOptions` | The `JsonSerializerOptions` to generate payload schemas from. By default each message's schema comes from the options of the serializer it's actually sent with. |
| `PostProcess` | An `Action<JsonObject>` called with the generated document before it's written, for anything the other options don't cover. |

```csharp
builder.Services.AddJustSayingAsyncApi(options =>
{
    options.Title = "Orders";
    options.Version = "2.1.0";
    options.Contact = new AsyncApiContactOptions { Name = "Orders team", Email = "orders@example.com" };
    options.License = new AsyncApiLicenseOptions { Name = "MIT" };
    options.Tags.Add(new AsyncApiTagOptions { Name = "orders" });
});
```

### Servers

When no servers are configured, the document lists the SNS and SQS endpoints of the regions your topics and queues are in. To describe your environments instead, add servers. Each topic is bound to the servers with the `sns` protocol, and each queue to the ones with `sqs`:

```csharp
options.Servers["production-sns"] = new AsyncApiServerOptions
{
    Host = "sns.eu-west-1.amazonaws.com",
    Protocol = "sns",
    Description = "Production",
};

options.Servers["production-sqs"] = new AsyncApiServerOptions
{
    Host = "sqs.eu-west-1.amazonaws.com",
    Protocol = "sqs",
    Description = "Production",
};
```

## What's in the document

* A channel for each topic the application publishes to, each queue it publishes to, and each queue it reads from. A topic subscription's queue says which topic it's subscribed to.
* Send and receive operations for those channels.
* Each message, named by its SNS `Subject` (or its `type` for a [CloudEvent](/cloudevents/)), with its content type and a JSON schema of its payload. CloudEvents are described with their envelope.

Payload schemas are generated from the System.Text.Json options each message is serialized with, so they match what's on the wire, including the [default options](/messages/serialization) and any source-generated context. A message sent with a serializer that doesn't describe its format, such as Newtonsoft.Json or a custom one, is documented without a payload schema; see [Writing a serializer](/messages/serialization#writing-a-serializer).

## Limitations

* **No SNS or SQS bindings.** Filter policies, queue attributes and cross-account topics aren't described, and the envelope a queue's messages arrive in (SNS notification, raw, or JustSaying's queue wrapper) is only explained in its operation's description.
* **Dynamic topics are left out.** A publication whose topic name is computed per message (`WithTopicName(message => ...)`) has no fixed address, so it's omitted, with a warning.
* **Not AOT-compatible.** The package writes documents with ByteBard.AsyncAPI.NET, which isn't annotated for trimming, so a trimmed or [Native AOT](/advanced/native-aot) application that references it gets IL2104 and IL3053 warnings.

Anything left out of the document is logged as a warning, with a code from JSAA101 to JSAA107. See [Build-time generation](build-time.md#warnings-and-errors) for the list.
