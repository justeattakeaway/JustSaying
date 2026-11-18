using System.Text.Json;
using JustSaying.CloudEvents;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// The bus serializes a message again on every publish attempt. A CloudEvents <c>id</c> and <c>time</c>
/// minted at serialization must still be the same across the retries of one publish, or a duplicate
/// delivery (the first attempt reached SNS, but its response didn't) looks like a different event.
/// </summary>
public class WhenRetryingACloudEventPublish
{
    private const string OrderPlacedType = "com.example.orders.order.placed";

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task Then_A_Bare_Payload_Keeps_Its_Id_And_Time_Across_Attempts()
    {
        var factory = CreateFactory();
        var publisher = new FailFirstAttemptPublisher(factory.GetDataOnlySerializer<OrderPlaced>(OrderPlacedType));
        using var bus = CreateBus(publisher);

        await bus.PublishAsync(new OrderPlaced { OrderId = "order-1" }, CancellationToken.None);

        AssertSameIdentity(publisher.Attempts);
    }

    [Test]
    public async Task Then_An_Envelope_Without_An_Id_Keeps_Its_Id_And_Time_Across_Attempts()
    {
        var factory = CreateFactory();
        var publisher = new FailFirstAttemptPublisher(factory.GetEnvelopeSerializer<OrderPlaced>(OrderPlacedType));
        using var bus = CreateBus(publisher);

        await bus.PublishAsync(new CloudEvent<OrderPlaced>(new OrderPlaced { OrderId = "order-1" }), CancellationToken.None);

        AssertSameIdentity(publisher.Attempts);
    }

    [Test]
    public async Task Then_A_Batch_Keeps_Each_Messages_Id_Across_Attempts()
    {
        var factory = CreateFactory();
        var publisher = new FailFirstAttemptPublisher(factory.GetDataOnlySerializer<OrderPlaced>(OrderPlacedType));
        using var bus = CreateBus(publisher);

        await bus.PublishBatchAsync([new OrderPlaced { OrderId = "order-1" }, new OrderPlaced { OrderId = "order-2" }], null, CancellationToken.None);

        publisher.Attempts.Count.ShouldBe(4);
        AssertSameIdentity([publisher.Attempts[0], publisher.Attempts[2]]);
        AssertSameIdentity([publisher.Attempts[1], publisher.Attempts[3]]);
        Id(publisher.Attempts[0]).ShouldNotBe(Id(publisher.Attempts[1]));
    }

    [Test]
    public async Task Then_Separate_Payloads_Get_Separate_Ids()
    {
        var serializer = CreateFactory().GetDataOnlySerializer<OrderPlaced>(OrderPlacedType);

        Id(serializer.Serialize(new OrderPlaced { OrderId = "order-1" }))
            .ShouldNotBe(Id(serializer.Serialize(new OrderPlaced { OrderId = "order-1" })));

        await Task.CompletedTask;
    }

    private static CloudEventSerializationFactory CreateFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessagingConfig>(new MessagingConfig());
        services.AddJustSayingCloudEvents(options => options.Source = new Uri("/orders", UriKind.Relative));

        return services.BuildServiceProvider().GetRequiredService<CloudEventSerializationFactory>();
    }

    private static JustSayingBus CreateBus(FailFirstAttemptPublisher publisher)
    {
        var config = new MessagingConfig
        {
            PublishFailureReAttempts = 2,
            PublishFailureBackoff = TimeSpan.Zero,
        };

        var bus = new JustSayingBus(config, new SystemTextJsonSerializationFactory(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions), NullLoggerFactory.Instance, new NullOpMessageMonitor());
        publisher.Register(bus);
        return bus;
    }

    private static void AssertSameIdentity(IReadOnlyList<string> bodies)
    {
        bodies.Count.ShouldBeGreaterThan(1);
        bodies.Select(Id).Distinct().Count().ShouldBe(1);
        bodies.Select(body => JsonDocument.Parse(body).RootElement.GetProperty("time").GetString()).Distinct().Count().ShouldBe(1);
    }

    private static string Id(string body) => JsonDocument.Parse(body).RootElement.GetProperty("id").GetString();

    /// <summary>
    /// Serializes each attempt as a real publisher does, and fails the first attempt of each publish.
    /// </summary>
    private sealed class FailFirstAttemptPublisher(object serializer) : IMessagePublisher, IMessageBatchPublisher
    {
        private bool _failed;

        public List<string> Attempts { get; } = [];

        public void Register(JustSayingBus bus)
        {
            if (serializer is IMessageBodySerializer<CloudEvent<OrderPlaced>>)
            {
                bus.AddMessagePublisher<CloudEvent<OrderPlaced>>(this);
            }
            else
            {
                bus.AddMessagePublisher<OrderPlaced>(this);
            }
        }

        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class
            => PublishAsync(message, null, cancellationToken);

        public Task PublishAsync<TMessage>(TMessage message, PublishMetadata metadata, CancellationToken cancellationToken) where TMessage : class
        {
            Attempts.Add(((IMessageBodySerializer<TMessage>)serializer).Serialize(message));
            return FailOnce();
        }

        public Task PublishBatchAsync<TMessage>(IEnumerable<TMessage> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken) where TMessage : class
        {
            Attempts.AddRange(messages.Select(((IMessageBodySerializer<TMessage>)serializer).Serialize));
            return FailOnce();
        }

        private Task FailOnce()
        {
            if (_failed)
            {
                return Task.CompletedTask;
            }

            _failed = true;
            return Task.FromException(new InvalidOperationException("The publish response was lost."));
        }

        public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public InterrogationResult Interrogate() => InterrogationResult.Empty;
    }
}
