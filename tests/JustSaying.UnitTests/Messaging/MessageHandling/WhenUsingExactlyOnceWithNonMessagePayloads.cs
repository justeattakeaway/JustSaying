using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace JustSaying.UnitTests.Messaging.MessageHandling;

public class WhenUsingExactlyOnceWithNonMessagePayloads
{
    private TextWriter OutputHelper => TestContext.Current!.OutputWriter;

    private sealed class PocoOrder
    {
        public string OrderRef { get; set; }
    }

    private sealed class Envelope<T>
    {
        public string Id { get; set; }

        public T Data { get; set; }
    }

    [Test]
    public void WhenTypeDoesNotDeriveFromMessageAndNoKeySelectorIsProvided_ThenRegistrationThrows()
    {
        var resolver = new InMemoryServiceResolver(sc => sc
            .AddLogging(l => l.AddTextWriter(OutputHelper))
            .AddSingleton<IMessageLockAsync>(new FakeMessageLock()));

        var builder = new HandlerMiddlewareBuilder(resolver, resolver);

        // A fresh GUID fallback would silently turn exactly-once into a no-op; we fail fast instead.
        var exception = Should.Throw<InvalidOperationException>(
            () => builder.UseExactlyOnce<PocoOrder>("poco-lock"));

        exception.Message.ShouldContain(nameof(PocoOrder));
        exception.Message.ShouldContain("deduplicationKeySelector");
    }

    [Test]
    public async Task WhenAKeySelectorIsProvided_ThenItIsUsedToFormTheLockKey()
    {
        var messageLock = new FakeMessageLock();

        var resolver = new InMemoryServiceResolver(sc => sc
            .AddLogging(l => l.AddTextWriter(OutputHelper))
            .AddSingleton<IMessageLockAsync>(messageLock));

        var handler = new InspectableHandler<PocoOrder>();

        var middleware = new HandlerMiddlewareBuilder(resolver, resolver)
            .UseExactlyOnce<PocoOrder>("poco-lock", deduplicationKeySelector: m => m.OrderRef)
            .UseHandler(ctx => handler)
            .Build();

        var context = new HandleMessageContext(
            "test-queue",
            new Message(),
            new PocoOrder { OrderRef = "order-123" },
            typeof(PocoOrder),
            new FakeVisibilityUpdater(),
            new FakeMessageDeleter(),
            new Uri("http://test-queue"),
            new MessageAttributes());

        var result = await middleware.RunAsync(context, null, CancellationToken.None);

        result.ShouldBeTrue();
        handler.ReceivedMessages.ShouldContain(x => x.OrderRef == "order-123");
        messageLock.MessageLockRequests.ShouldContain(r => r.key.StartsWith("order-123-", StringComparison.Ordinal));

        // A non-generic type's key is unchanged from v8, so in-flight locks survive the upgrade.
        messageLock.MessageLockRequests.ShouldContain(
            r => r.key == $"order-123-{typeof(PocoOrder).FullName.ToLowerInvariant()}-poco-lock");
    }

    [Test]
    public async Task WhenThePayloadIsGeneric_ThenTheLockKeyDoesNotDependOnAssemblyVersions()
    {
        var messageLock = new FakeMessageLock();

        var resolver = new InMemoryServiceResolver(sc => sc
            .AddLogging(l => l.AddTextWriter(OutputHelper))
            .AddSingleton<IMessageLockAsync>(messageLock));

        var middleware = new HandlerMiddlewareBuilder(resolver, resolver)
            .UseExactlyOnce<Envelope<PocoOrder>>("poco-lock", deduplicationKeySelector: m => m.Id)
            .UseHandler(ctx => new InspectableHandler<Envelope<PocoOrder>>())
            .Build();

        var context = new HandleMessageContext(
            "test-queue",
            new Message(),
            new Envelope<PocoOrder> { Id = "event-1", Data = new PocoOrder { OrderRef = "order-123" } },
            typeof(Envelope<PocoOrder>),
            new FakeVisibilityUpdater(),
            new FakeMessageDeleter(),
            new Uri("http://test-queue"),
            new MessageAttributes());

        await middleware.RunAsync(context, null, CancellationToken.None);

        // A generic type's FullName embeds "Version=..." for its type arguments, which would change the
        // key on every deploy; the key uses the readable C# spelling instead.
        var envelopeName = typeof(Envelope<>).FullName.Split('`')[0];
        var expectedKey = $"event-1-{envelopeName}<{typeof(PocoOrder).FullName}>-poco-lock".ToLowerInvariant();
        messageLock.MessageLockRequests.ShouldContain(r => r.key == expectedKey);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("  ")]
    public async Task WhenTheKeySelectorReturnsAnEmptyKey_ThenTheMessageIsLeftOnTheQueue(string orderRef)
    {
        var messageLock = new FakeMessageLock();
        var logs = new FakeLogCollector();

        var resolver = new InMemoryServiceResolver(sc => sc
            .AddLogging(l => l.AddTextWriter(OutputHelper).AddProvider(new FakeLoggerProvider(logs)))
            .AddSingleton<IMessageLockAsync>(messageLock));

        var handler = new InspectableHandler<PocoOrder>();

        var middleware = new HandlerMiddlewareBuilder(resolver, resolver)
            .UseExactlyOnce<PocoOrder>("poco-lock", deduplicationKeySelector: m => m.OrderRef)
            .UseHandler(ctx => handler)
            .Build();

        var context = new HandleMessageContext(
            "test-queue",
            new Message { MessageId = "sqs-message-1" },
            new PocoOrder { OrderRef = orderRef },
            typeof(PocoOrder),
            new FakeVisibilityUpdater(),
            new FakeMessageDeleter(),
            new Uri("http://test-queue"),
            new MessageAttributes());

        // An empty key would make every such payload share one lock, silently deduplicating
        // unrelated messages, so the message is declined rather than handled.
        var result = await middleware.RunAsync(context, null, CancellationToken.None);

        result.ShouldBeFalse();
        handler.ReceivedMessages.ShouldBeEmpty();
        messageLock.MessageLockRequests.ShouldBeEmpty();

        var error = logs.GetSnapshot().Where(r => r.Level == LogLevel.Error).ShouldHaveSingleItem();
        error.Message.ShouldContain(typeof(PocoOrder).FullName);
        error.Message.ShouldContain("sqs-message-1");
    }

    [Test]
    public async Task WhenTheMessageIsNotOfTheConfiguredType_ThenTheMessageIsLeftOnTheQueue()
    {
        var messageLock = new FakeMessageLock();
        var logs = new FakeLogCollector();

        var resolver = new InMemoryServiceResolver(sc => sc
            .AddLogging(l => l.AddTextWriter(OutputHelper).AddProvider(new FakeLoggerProvider(logs)))
            .AddSingleton<IMessageLockAsync>(messageLock));

        var middleware = new HandlerMiddlewareBuilder(resolver, resolver)
            .UseExactlyOnce<PocoOrder>("poco-lock", deduplicationKeySelector: m => m.OrderRef)
            .Build();

        var context = new HandleMessageContext(
            "test-queue",
            new Message { MessageId = "sqs-message-1" },
            new SimpleMessage(),
            typeof(SimpleMessage),
            new FakeVisibilityUpdater(),
            new FakeMessageDeleter(),
            new Uri("http://test-queue"),
            new MessageAttributes());

        var result = await middleware.RunAsync(context, null, CancellationToken.None);

        result.ShouldBeFalse();
        messageLock.MessageLockRequests.ShouldBeEmpty();

        var error = logs.GetSnapshot().Where(r => r.Level == LogLevel.Error).ShouldHaveSingleItem();
        error.Message.ShouldContain(typeof(SimpleMessage).FullName);
    }
}
