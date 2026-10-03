---
---

# Serialization

JustSaying serializes message bodies with **System.Text.Json** by default. `AddJustSaying` registers a `SystemTextJsonSerializationFactory` using `SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions`, which:

* leaves `null` properties out when writing;
* reads property names case-insensitively;
* includes public fields;
* populates get-only collection and object properties when reading;
* doesn't escape non-ASCII or HTML-sensitive characters (message bodies are never embedded in HTML, and escaping can double or triple their size);
* writes enums as strings (`"Status":"Paid"`), and still reads numbers.

These keep the wire format and binding close to Newtonsoft.Json, which was the default before v9. For the differences that remain, see [Upgrading to v9](/upgrading-to-v9).

```json
{"OrderId":"order-1","Status":"Paid","Total":12.5}
```

:::note
Enums are written as strings by a `JsonStringEnumConverter`, which needs dynamic code. Under [Native AOT](/advanced/native-aot) it isn't added, so a source-generated context must set `UseStringEnumConverter = true` to match.
:::

## Customising the options

Register your own `SystemTextJsonSerializationFactory`. Start from a copy of the defaults so you keep their behaviour:

```csharp
var options = new JsonSerializerOptions(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
{
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
};

services.AddSingleton<IMessageBodySerializationFactory>(new SystemTextJsonSerializationFactory(options));

services.AddJustSaying(config => config.Messaging(x => x.WithRegion("eu-west-1")));
```

`AddJustSaying` registers its default with `TryAddSingleton`, so a factory registered with `AddSingleton` wins whether it comes before or after `AddJustSaying`.

Every service that reads your messages has to understand what you write, so change the options with care. Use the `System.Text.Json.Serialization` attributes (`[JsonPropertyName]`, `[JsonIgnore]`, `[JsonConverter]`) on message types, not the Newtonsoft.Json ones, which System.Text.Json ignores.

## Using Newtonsoft.Json

Newtonsoft.Json is still supported, as an opt-in:

```csharp
// Exactly the v8 wire format: enums as numbers, null properties written.
services.AddSingleton<IMessageBodySerializationFactory>(
    new NewtonsoftSerializationFactory(new JsonSerializerSettings()));
```

`new NewtonsoftSerializationFactory()` with no settings writes enums as strings and leaves out `null` properties, like the System.Text.Json default. Pass `new JsonSerializerSettings()`, or the settings you used before, to keep the v8 wire format byte for byte.

With StructureMap, the last registration wins, so register the factory **after** `AddJustSaying`:

```csharp
var container = new Container(registry =>
{
    registry.AddJustSaying("eu-west-1");
    registry.For<IMessageBodySerializationFactory>()
            .Use(new NewtonsoftSerializationFactory())
            .Singleton();
});
```

Newtonsoft.Json can't be used under Native AOT.

## A different serializer for one subscription

`ForTopic<T>` and `ForQueue<T>` subscriptions can read their messages with their own serializer, leaving the app-wide one alone:

```csharp
x.ForTopic<OrderPlaced>(c => c.WithMessageBodySerializer(new LegacyOrderSerializer()));
```

## Writing a serializer

A serializer implements `IMessageBodySerializer<T>`, and a factory that creates one for each message type implements `IMessageBodySerializationFactory`:

```csharp
public sealed class LegacyOrderSerializer : IMessageBodySerializer<OrderPlaced>
{
    public string Serialize(OrderPlaced message)
        => string.Create(CultureInfo.InvariantCulture, $"{message.OrderId}|{message.Total}");

    public OrderPlaced Deserialize(string message)
    {
        var parts = message.Split('|');
        return new OrderPlaced(parts[0], decimal.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}
```

The serializer only deals with the message body. JustSaying adds and removes the SNS and queue envelopes around it, so the body can be anything that fits in a string.

To have [AsyncAPI generation](/asyncapi/) describe what a custom serializer writes, also implement `IMessageBodyFormat` (its `ContentType`). If it uses System.Text.Json, implement `ISystemTextJsonMessageBodySerializer` instead, which adds its `SerializerOptions`, so that payload schemas can be generated from them. Messages read or written by a serializer that implements neither are documented without a payload schema.
