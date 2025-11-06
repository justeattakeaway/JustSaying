# Configuration

All publication configuration can be accessed via the `MessagingBusBuilder.Publications` fluent api:

```csharp
services.AddJustSaying((MessagingBusBuilder config) =>
{
    config.Publications((PublicationsBuilder publicationConfig) =>
    {
        // here
    });

});
```

The `publicationConfig` builder provides methods to describe the topology of your messaging setup. 

They are essentially the same as the subscription configuration settings, except that the only define the topology for publishing messages, and not receiving them.

