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
public sealed class AotRoundTripTests
{
    [Test]
    [Timeout(30_000)]
    public async Task Published_Message_Is_Received_Under_Native_Aot(CancellationToken cancellationToken)
    {
        var bus = new InMemoryAwsBus();
        var signal = new MessageReceivedSignal();

        var serializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = AotTestSerializerContext.Default,
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(signal);

        // The default System.Text.Json factory uses reflection-based options (no source-gen
        // resolver) and throws under Native AOT. Register a source-generated factory first so it
        // wins the TryAdd in AddJustSaying.
        services.TryAddSingleton<IMessageBodySerializationFactory>(
            _ => new SystemTextJsonSerializationFactory(serializerOptions));

        ConfigureJustSaying(services, bus);

        await using var provider = services.BuildServiceProvider();

        var publisher = provider.GetRequiredService<IMessagePublisher>();
        var listener = provider.GetRequiredService<IMessagingBus>();

        await listener.StartAsync(cancellationToken);
        await publisher.StartAsync(cancellationToken);

        await publisher.PublishAsync(new TestMessage { Content = "hello-aot" }, cancellationToken);

        var received = await signal.Received.Task.WaitAsync(cancellationToken);

        await Assert.That(received.Content).IsEqualTo("hello-aot");
    }

    private static void ConfigureJustSaying(IServiceCollection services, InMemoryAwsBus bus)
    {
        services.AddJustSaying(config =>
        {
            config.Messaging(x => x.WithRegion("eu-west-1"))
                  .Client(x => x.WithClientFactory(() => new InMemoryAwsClientFactory(bus)));
            config.Publications(x => x.WithTopic<TestMessage>());
            config.Subscriptions(x => x.ForTopic<TestMessage>(
                sub => sub.WithQueueName("aot-test-queue")));
        });

        services.AddJustSayingHandler<TestMessage, TestMessageHandler>();
    }
}

public sealed class TestMessage : Message
{
    public string Content { get; set; }
}

public sealed class TestMessageHandler(MessageReceivedSignal signal) : IHandlerAsync<TestMessage>
{
    public Task<bool> Handle(TestMessage message)
    {
        signal.Received.TrySetResult(message);
        return Task.FromResult(true);
    }
}

/// <summary>
/// Shared signal used to surface the handled message back to the test without
/// needing to reach into the DI-constructed handler instance.
/// </summary>
public sealed class MessageReceivedSignal
{
    public TaskCompletionSource<TestMessage> Received { get; } =
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

[JsonSerializable(typeof(TestMessage))]
public sealed partial class AotTestSerializerContext : JsonSerializerContext;
