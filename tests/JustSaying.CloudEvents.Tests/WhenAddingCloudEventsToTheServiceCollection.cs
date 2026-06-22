using System.Text.Json;
using JustSaying;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.CloudEvents.Tests;

public class WhenAddingCloudEventsToTheServiceCollection
{
    private sealed class OrderPlaced : Message
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task ItRegistersACloudEventSerializationFactoryThatEmitsTheConfiguredType()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessagingConfig>(new MessagingConfig());
        services.AddJustSayingCloudEvents(options =>
        {
            options.Source = new Uri("https://orders.example.com/");
            options.WithCloudEventType<OrderPlaced>("com.justeattakeaway.orders.orderplaced");
        });

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IMessageBodySerializationFactory>();

        await Assert.That(factory).IsTypeOf<CloudEventSerializationFactory>();

        using var doc = JsonDocument.Parse(factory.GetSerializer<OrderPlaced>().Serialize(new OrderPlaced { OrderId = "1" }));
        await Assert.That(doc.RootElement.GetProperty("type").GetString()).IsEqualTo("com.justeattakeaway.orders.orderplaced");
    }
}
