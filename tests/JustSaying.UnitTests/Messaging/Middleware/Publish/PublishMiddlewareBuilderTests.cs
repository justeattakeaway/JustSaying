using JustSaying.Messaging.Middleware;
using JustSaying.UnitTests.Messaging.Channels.Fakes;

namespace JustSaying.UnitTests.Messaging.Middleware.Publish;

public class PublishMiddlewareBuilderTests
{
    [Fact]
    public async Task Build_WithNoMiddleware_ReturnsPassthroughChain()
    {
        var resolver = new InMemoryServiceResolver();
        var builder = new PublishMiddlewareBuilder(resolver);

        var middleware = builder.Build();

        var result = await middleware.RunAsync(
            CreateContext(),
            _ => Task.FromResult(true),
            CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task Use_WithInstance_AddsMiddlewareToChain()
    {
        var resolver = new InMemoryServiceResolver();
        var invoked = false;
        var tracking = new TrackingPublishMiddleware(() => invoked = true);

        var middleware = new PublishMiddlewareBuilder(resolver)
            .Use(tracking)
            .Build();

        await middleware.RunAsync(
            CreateContext(),
            _ => Task.FromResult(true),
            CancellationToken.None);

        invoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Use_WithFactory_InvokesFactory()
    {
        var resolver = new InMemoryServiceResolver();
        var invoked = false;

        var middleware = new PublishMiddlewareBuilder(resolver)
            .Use(() => new TrackingPublishMiddleware(() => invoked = true))
            .Build();

        await middleware.RunAsync(
            CreateContext(),
            _ => Task.FromResult(true),
            CancellationToken.None);

        invoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Configure_DefersExecution()
    {
        var resolver = new InMemoryServiceResolver();
        var invoked = false;

        var builder = new PublishMiddlewareBuilder(resolver)
            .Configure(pipe => pipe.Use(new TrackingPublishMiddleware(() => invoked = true)));

        invoked.ShouldBeFalse();

        var middleware = builder.Build();
        await middleware.RunAsync(
            CreateContext(),
            _ => Task.FromResult(true),
            CancellationToken.None);

        invoked.ShouldBeTrue();
    }

    [Fact]
    public async Task DeclarationOrder_MatchesExecutionOrder()
    {
        var resolver = new InMemoryServiceResolver();
        var callOrder = new List<string>();

        var middleware = new PublishMiddlewareBuilder(resolver)
            .Use(new OrderTrackingMiddleware("first", callOrder))
            .Use(new OrderTrackingMiddleware("second", callOrder))
            .Use(new OrderTrackingMiddleware("third", callOrder))
            .Build();

        await middleware.RunAsync(
            CreateContext(),
            _ =>
            {
                callOrder.Add("inner");
                return Task.FromResult(true);
            },
            CancellationToken.None);

        callOrder.ShouldBe(["first", "second", "third", "inner"]);
    }

    private static PublishContext CreateContext()
    {
        return new PublishContext(
            new TestingFramework.SimpleMessage(),
            new JustSaying.Messaging.PublishMetadata());
    }

    private class TrackingPublishMiddleware(Action onInvoked) : MiddlewareBase<PublishContext, bool>
    {
        protected override async Task<bool> RunInnerAsync(
            PublishContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
        {
            onInvoked();
            return await func(stoppingToken).ConfigureAwait(false);
        }
    }

    private class OrderTrackingMiddleware(string name, List<string> callOrder) : MiddlewareBase<PublishContext, bool>
    {
        protected override async Task<bool> RunInnerAsync(
            PublishContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
        {
            callOrder.Add(name);
            return await func(stoppingToken).ConfigureAwait(false);
        }
    }
}
