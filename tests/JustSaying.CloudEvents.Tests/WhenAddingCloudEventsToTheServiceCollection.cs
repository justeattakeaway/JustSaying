using System.Text.Json;
using JustSaying;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            options.MapType<OrderPlaced>("com.justeattakeaway.orders.orderplaced");
        });

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<CloudEventSerializationFactory>();

        using var doc = JsonDocument.Parse(factory.GetSerializer<OrderPlaced>().Serialize(new OrderPlaced { OrderId = "1" }));
        await Assert.That(doc.RootElement.GetProperty("type").GetString()).IsEqualTo("com.justeattakeaway.orders.orderplaced");
    }

    [Test]
    public async Task ItDoesNotReplaceTheAppWideSerializationFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessagingConfig>(new MessagingConfig());
        services.AddSingleton<IMessageBodySerializationFactory>(
            new SystemTextJsonSerializationFactory(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions));
        services.AddJustSayingCloudEvents();

        await using var provider = services.BuildServiceProvider();

        await Assert.That(provider.GetRequiredService<IMessageBodySerializationFactory>())
            .IsTypeOf<SystemTextJsonSerializationFactory>();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task UseAsDefaultMakesCloudEventsTheAppWideFactoryInEitherRegistrationOrder(bool cloudEventsFirst)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessagingConfig>(new MessagingConfig());

        // AddJustSaying registers the System.Text.Json default with TryAdd; the CloudEvents opt-in
        // must win whether it runs before or after it.
        void AddDefaultFactory() => services.TryAddSingleton<IMessageBodySerializationFactory>(
            new SystemTextJsonSerializationFactory(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions));
        void AddCloudEvents() => services.AddJustSayingCloudEvents(
            options => options.Source = new Uri("https://orders.example.com/"),
            useAsDefault: true);

        if (cloudEventsFirst)
        {
            AddCloudEvents();
            AddDefaultFactory();
        }
        else
        {
            AddDefaultFactory();
            AddCloudEvents();
        }

        await using var provider = services.BuildServiceProvider();

        var appWideFactory = provider.GetRequiredService<IMessageBodySerializationFactory>();
        await Assert.That(appWideFactory).IsTypeOf<CloudEventSerializationFactory>();
        await Assert.That(appWideFactory).IsSameReferenceAs(provider.GetRequiredService<CloudEventSerializationFactory>());
    }
}
