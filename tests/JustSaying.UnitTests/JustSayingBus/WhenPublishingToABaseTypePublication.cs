using System.Collections;
using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

/// <summary>
/// A message with no publication registered for its runtime type is published to the publication for
/// its closest base class, otherwise to the one for an interface it implements. A publication for the
/// exact runtime type always wins.
/// </summary>
public class WhenPublishingToABaseTypePublication : GivenAServiceBus
{
    public interface IOrderEvent;

    public interface IAuditable;

    public abstract class OrderEvent : IOrderEvent;

    public class OrderPlaced : OrderEvent, IAuditable;

    public sealed class OrderShipped : OrderPlaced;

    public sealed class Refund : IOrderEvent, IAuditable;

    protected override Task WhenAsync() => Task.CompletedTask;

    [Test]
    public async Task ADerivedMessageIsPublishedToTheClosestBaseClassPublication()
    {
        var orderEventPublisher = AddPublisher<OrderEvent>();
        var orderPlacedPublisher = AddPublisher<OrderPlaced>();

        await SystemUnderTest.PublishAsync(new OrderShipped());

        PublishedMessages(orderPlacedPublisher).ShouldHaveSingleItem().ShouldBeOfType<OrderShipped>();
        PublishedMessages(orderEventPublisher).ShouldBeEmpty();
    }

    [Test]
    public async Task APublicationForTheExactTypeWins()
    {
        var orderEventPublisher = AddPublisher<OrderEvent>();
        var orderPlacedPublisher = AddPublisher<OrderPlaced>();

        await SystemUnderTest.PublishAsync(new OrderPlaced());

        PublishedMessages(orderPlacedPublisher).ShouldHaveSingleItem().ShouldBeOfType<OrderPlaced>();
        PublishedMessages(orderEventPublisher).ShouldBeEmpty();
    }

    [Test]
    public async Task ABaseClassPublicationWinsOverAnInterfacePublication()
    {
        var interfacePublisher = AddPublisher<IOrderEvent>();
        var orderEventPublisher = AddPublisher<OrderEvent>();

        await SystemUnderTest.PublishAsync(new OrderPlaced());

        PublishedMessages(orderEventPublisher).ShouldHaveSingleItem().ShouldBeOfType<OrderPlaced>();
        PublishedMessages(interfacePublisher).ShouldBeEmpty();
    }

    [Test]
    public async Task AnInterfacePublicationIsUsedWhenNoClassMatches()
    {
        var interfacePublisher = AddPublisher<IOrderEvent>();

        await SystemUnderTest.PublishAsync(new Refund());

        PublishedMessages(interfacePublisher).ShouldHaveSingleItem().ShouldBeOfType<Refund>();
    }

    [Test]
    public async Task TheMiddlewareForTheBaseTypePublicationIsUsed()
    {
        AddPublisher<OrderEvent>();
        var seen = new List<object>();
        SystemUnderTest.AddPublishMiddleware<OrderEvent>(new RecordingMiddleware(seen));

        await SystemUnderTest.PublishAsync(new OrderPlaced());

        seen.ShouldHaveSingleItem().ShouldBeOfType<OrderPlaced>();
    }

    [Test]
    public async Task MoreThanOneMatchingInterfacePublicationThrows()
    {
        AddPublisher<IOrderEvent>();
        AddPublisher<IAuditable>();

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => SystemUnderTest.PublishAsync(new Refund()));

        exception.Message.ShouldContain(typeof(Refund).ToString());
        exception.Message.ShouldContain(typeof(IOrderEvent).ToString());
        exception.Message.ShouldContain(typeof(IAuditable).ToString());
        exception.Message.ShouldContain("ambiguous");
    }

    [Test]
    public async Task MoreThanOneMatchingInterfacePublicationThrowsForABatch()
    {
        AddPublisher<IOrderEvent>();
        AddPublisher<IAuditable>();

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => SystemUnderTest.PublishBatchAsync([new Refund()], null, CancellationToken.None));

        exception.Message.ShouldContain("ambiguous");
    }

    [Test]
    public async Task ABatchIsGroupedByTheResolvedPublication()
    {
        var interfacePublisher = AddPublisher<IOrderEvent>();
        var orderEventPublisher = AddPublisher<OrderEvent>();
        var seen = new List<object>();
        SystemUnderTest.AddPublishMiddleware<OrderEvent>(new RecordingMiddleware(seen));

        await SystemUnderTest.PublishBatchAsync<IOrderEvent>(
            [new OrderPlaced(), new Refund(), new OrderShipped()],
            null,
            CancellationToken.None);

        var orderEventBatch = PublishedBatches(orderEventPublisher).ShouldHaveSingleItem();
        orderEventBatch.Count.ShouldBe(2);
        orderEventBatch[0].ShouldBeOfType<OrderPlaced>();
        orderEventBatch[1].ShouldBeOfType<OrderShipped>();

        PublishedBatches(interfacePublisher).ShouldHaveSingleItem().ShouldHaveSingleItem().ShouldBeOfType<Refund>();

        seen.Count.ShouldBe(2);
        seen.ShouldNotContain(message => message is Refund);
    }

    private IMessagePublisher AddPublisher<T>() where T : class
    {
        var publisher = Substitute.For<IMessagePublisher, IMessageBatchPublisher>();
        SystemUnderTest.AddMessagePublisher<T>(publisher);
        return publisher;
    }

    private static List<object> PublishedMessages(IMessagePublisher publisher)
        => publisher.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IMessagePublisher.PublishAsync))
            .Select(call => call.GetArguments()[0])
            .ToList();

    private static List<List<object>> PublishedBatches(IMessagePublisher publisher)
        => publisher.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IMessageBatchPublisher.PublishBatchAsync))
            .Select(call => ((IEnumerable)call.GetArguments()[0]).Cast<object>().ToList())
            .ToList();

    private sealed class RecordingMiddleware(List<object> seen) : MiddlewareBase<PublishContext, bool>
    {
        protected override async Task<bool> RunInnerAsync(
            PublishContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
        {
            lock (seen)
            {
                if (context.Messages is { } messages)
                {
                    seen.AddRange(messages);
                }
                else
                {
                    seen.Add(context.Message);
                }
            }

            return await func(stoppingToken).ConfigureAwait(false);
        }
    }
}
