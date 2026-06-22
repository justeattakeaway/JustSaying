using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.MessageHandling;

public class WhenUsingExactlyOnceWithNonMessagePayloads
{
    private TextWriter OutputHelper => TestContext.Current!.OutputWriter;

    private sealed class PocoOrder
    {
        public string OrderRef { get; set; }
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
    }
}
