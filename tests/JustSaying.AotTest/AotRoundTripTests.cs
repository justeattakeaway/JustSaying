using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JustSaying.AotTest;

/// <summary>
/// Exercises JustSaying's full publish -> subscribe -> handle round trip against the
/// in-memory <see cref="InMemoryAwsBus"/> from LocalSqsSnsMessaging. When this test
/// project is published with <c>PublishAot=true</c> and the resulting native binary
/// is run, a pass proves that the configuration, message-pump, and (source-generated)
/// System.Text.Json serialization paths all survive Native AOT without needing any
/// external AWS services.
/// </summary>
/// <remarks>
/// <c>PublishAot=true</c> also disables reflection-based System.Text.Json in the project's
/// runtimeconfig, so <c>dotnet test</c> on this project reproduces the AOT serializer behaviour
/// under JIT.
/// </remarks>
public sealed class AotRoundTripTests
{
    [Test]
    [Timeout(30_000)]
    public async Task Published_Message_Is_Received_Under_Native_Aot(CancellationToken cancellationToken)
    {
        await using var provider = BuildServiceProvider(new InMemoryAwsBus(), CreateSerializationFactory());

        var (publisher, _) = await StartAsync(provider, cancellationToken);

        await publisher.PublishAsync(new TestMessage { Content = "hello-aot" }, cancellationToken);

        var received = await provider.GetRequiredService<MessageReceivedSignal<TestMessage>>().Received.Task.WaitAsync(cancellationToken);

        await Assert.That(received.Content).IsEqualTo("hello-aot");
    }

    [Test]
    [Timeout(30_000)]
    public async Task Published_Record_Not_Deriving_From_Message_Is_Received_Under_Native_Aot(CancellationToken cancellationToken)
    {
        await using var provider = BuildServiceProvider(new InMemoryAwsBus(), CreateSerializationFactory());

        var (publisher, _) = await StartAsync(provider, cancellationToken);

        await publisher.PublishAsync(new OrderPlaced("order-1", OrderStatus.Paid), cancellationToken);

        var received = await provider.GetRequiredService<MessageReceivedSignal<OrderPlaced>>().Received.Task.WaitAsync(cancellationToken);

        await Assert.That(received).IsEqualTo(new OrderPlaced("order-1", OrderStatus.Paid));
    }

    [Test]
    public async Task Enums_Are_Written_As_Strings_Like_Under_Jit()
    {
        // Under JIT the default options write enums as strings; a source-generated context must opt in
        // with UseStringEnumConverter so that JIT and AOT services agree on the wire.
        var serializer = CreateSerializationFactory().GetSerializer<OrderPlaced>();

        var json = serializer.Serialize(new OrderPlaced("order-1", OrderStatus.Paid));

        await Assert.That(json).Contains("\"Status\":\"Paid\"");
        await Assert.That(serializer.Deserialize(json).Status).IsEqualTo(OrderStatus.Paid);

        // Numbers, as written by v8's Newtonsoft default, are still read.
        await Assert.That(serializer.Deserialize("""{"OrderId":"order-1","Status":1}""").Status).IsEqualTo(OrderStatus.Paid);
    }

    [Test]
    public async Task Copying_The_Default_Options_Keeps_Their_Leniency()
    {
        var serializer = CreateSerializationFactory().GetSerializer<TestMessage>();

        var message = serializer.Deserialize("""{"content":"camelCase","tags":["a","b"]}""");

        await Assert.That(message.Content).IsEqualTo("camelCase");
        await Assert.That(string.Join(",", message.Tags)).IsEqualTo("a,b");
        await Assert.That(serializer.Serialize(new TestMessage { Content = "日本語 & <b>" })).Contains("\"Content\":\"日本語 & <b>\"");
    }

    [Test]
    [Timeout(30_000)]
    public async Task Heterogeneous_Batch_Is_Received_Under_Native_Aot(CancellationToken cancellationToken)
    {
        await using var provider = BuildServiceProvider(new InMemoryAwsBus(), CreateSerializationFactory());

        var (_, batchPublisher) = await StartAsync(provider, cancellationToken);

        await batchPublisher.PublishBatchAsync<object>(
            [new TestMessage { Content = "batched" }, new OrderPlaced("order-2", OrderStatus.New)],
            cancellationToken);

        var message = await provider.GetRequiredService<MessageReceivedSignal<TestMessage>>().Received.Task.WaitAsync(cancellationToken);
        var order = await provider.GetRequiredService<MessageReceivedSignal<OrderPlaced>>().Received.Task.WaitAsync(cancellationToken);

        await Assert.That(message.Content).IsEqualTo("batched");
        await Assert.That(order).IsEqualTo(new OrderPlaced("order-2", OrderStatus.New));
    }

    [Test]
    public async Task A_Message_Type_Missing_From_The_Context_Fails_At_Bus_Build()
    {
        var options = new JsonSerializerOptions(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
        {
            TypeInfoResolver = TestMessageOnlySerializerContext.Default,
        };

        await using var provider = BuildServiceProvider(new InMemoryAwsBus(), new SystemTextJsonSerializationFactory(options));

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IMessagePublisher>());

        await Assert.That(exception.Message).Contains(typeof(OrderPlaced).FullName!);
        await Assert.That(exception.Message).Contains(nameof(SystemTextJsonSerializationFactory));
    }

    [Test]
    public async Task A_Dynamic_Topic_For_A_Type_Missing_From_The_Context_Fails_At_Bus_Build()
    {
        var options = new JsonSerializerOptions(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
        {
            TypeInfoResolver = TestMessageOnlySerializerContext.Default,
        };

        var bus = new InMemoryAwsBus();
        var services = new ServiceCollection();
        services.AddLogging();
        services.TryAddSingleton<IMessageBodySerializationFactory>(new SystemTextJsonSerializationFactory(options));
        services.AddJustSaying(config =>
        {
            config.Messaging(x => x.WithRegion("eu-west-1"))
                  .Client(x => x.WithClientFactory(() => new InMemoryAwsClientFactory(bus)));
            config.Publications(x => x.WithTopic<OrderPlaced>(topic => topic.WithTopicName(order => $"orders-{order.Status}")));
        });

        await using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IMessagePublisher>());

        await Assert.That(exception.Message).Contains(typeof(OrderPlaced).FullName!);
    }

    [Test]
    public async Task The_Default_Serializer_Without_A_Context_Fails_At_Bus_Build()
    {
        await using var provider = BuildServiceProvider(new InMemoryAwsBus(), serializationFactory: null);

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IMessagingBus>());

        await Assert.That(exception.Message).Contains("JsonSerializerContext");
    }

    private static SystemTextJsonSerializationFactory CreateSerializationFactory()
    {
        // Copy JustSaying's defaults so the source-generated path reads and writes like the JIT default.
        var options = new JsonSerializerOptions(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
        {
            TypeInfoResolver = AotTestSerializerContext.Default,
        };

        return new SystemTextJsonSerializationFactory(options);
    }

    private static ServiceProvider BuildServiceProvider(InMemoryAwsBus bus, IMessageBodySerializationFactory serializationFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MessageReceivedSignal<TestMessage>>();
        services.AddSingleton<MessageReceivedSignal<OrderPlaced>>();

        // The default System.Text.Json factory uses reflection-based options (no source-gen
        // resolver) and can't serialize anything under Native AOT. Register a source-generated
        // factory first so it wins the TryAdd in AddJustSaying.
        if (serializationFactory is not null)
        {
            services.TryAddSingleton(serializationFactory);
        }

        services.AddJustSaying(config =>
        {
            config.Messaging(x => x.WithRegion("eu-west-1"))
                  .Client(x => x.WithClientFactory(() => new InMemoryAwsClientFactory(bus)));
            config.Publications(x =>
            {
                x.WithTopic<TestMessage>();
                x.WithTopic<OrderPlaced>();
            });
            config.Subscriptions(x =>
            {
                x.ForTopic<TestMessage>(sub => sub.WithQueueName("aot-test-queue"));
                x.ForTopic<OrderPlaced>(sub => sub.WithQueueName("aot-test-orders"));
            });
        });

        services.AddJustSayingHandler<TestMessage, SignallingHandler<TestMessage>>();
        services.AddJustSayingHandler<OrderPlaced, SignallingHandler<OrderPlaced>>();

        return services.BuildServiceProvider();
    }

    private static async Task<(IMessagePublisher Publisher, IMessageBatchPublisher BatchPublisher)> StartAsync(
        ServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var publisher = provider.GetRequiredService<IMessagePublisher>();
        var batchPublisher = provider.GetRequiredService<IMessageBatchPublisher>();
        var listener = provider.GetRequiredService<IMessagingBus>();

        await listener.StartAsync(cancellationToken);
        await publisher.StartAsync(cancellationToken);

        return (publisher, batchPublisher);
    }
}

public sealed class TestMessage : Message
{
    public string Content { get; set; }

    public List<string> Tags { get; } = [];
}

public enum OrderStatus
{
    New,
    Paid,
}

/// <summary>
/// A message that doesn't derive from <see cref="Message"/>.
/// </summary>
public sealed record OrderPlaced(string OrderId, OrderStatus Status);

public sealed class SignallingHandler<T>(MessageReceivedSignal<T> signal) : IHandlerAsync<T>
    where T : class
{
    public Task<bool> Handle(T message)
    {
        signal.Received.TrySetResult(message);
        return Task.FromResult(true);
    }
}

/// <summary>
/// Shared signal used to surface the handled message back to the test without
/// needing to reach into the DI-constructed handler instance.
/// </summary>
public sealed class MessageReceivedSignal<T>
{
    public TaskCompletionSource<T> Received { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Adapts the in-memory LocalSqsSnsMessaging bus to JustSaying's <see cref="IAwsClientFactory"/>
/// so the whole round trip stays in-process.
/// </summary>
public sealed class InMemoryAwsClientFactory(InMemoryAwsBus bus) : IAwsClientFactory
{
    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region) => bus.CreateSnsClient();

    public IAmazonSQS GetSqsClient(RegionEndpoint region) => bus.CreateSqsClient();
}

// JustSaying's JIT default writes enums as strings, but its JsonStringEnumConverter needs dynamic
// code, so it isn't added under Native AOT. Without UseStringEnumConverter a source-generated
// context writes enums as numbers, and can't read the strings that JIT (and v8 Newtonsoft)
// services send.
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(TestMessage))]
[JsonSerializable(typeof(OrderPlaced))]
public sealed partial class AotTestSerializerContext : JsonSerializerContext;

[JsonSerializable(typeof(TestMessage))]
public sealed partial class TestMessageOnlySerializerContext : JsonSerializerContext;
