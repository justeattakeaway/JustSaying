using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Models;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

namespace JustSaying.UnitTests.Messaging.Middleware;

public class MiddlewareMapTests
{
    [Test]
    public void EmptyMapDoesNotContain()
    {
        var map = CreateMiddlewareMap();
        map.Contains("queue", typeof(SimpleMessage)).ShouldBeFalse();
    }

    [Test]
    public void EmptyMapReturnsNullMiddleware()
    {
        var map = CreateMiddlewareMap();

        var handler = map.Get("queue", typeof(SimpleMessage));

        handler.ShouldBeNull();
    }

    [Test]
    public void MiddlewareIsReturnedForMatchingType()
    {
        var map = CreateMiddlewareMap();

        var middleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        map.Add<SimpleMessage>("queue",  middleware);

        var handler = map.Get("queue", typeof(SimpleMessage));

        handler.ShouldNotBeNull();
    }

    [Test]
    public void MiddlewareContainsKeyForMatchingTypeOnly()
    {
        var map = CreateMiddlewareMap();
        var middleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        map.Add<SimpleMessage>("queue", middleware);

        map.Contains("queue", typeof(SimpleMessage)).ShouldBeTrue();
        map.Contains("queue", typeof(AnotherSimpleMessage)).ShouldBeFalse();
    }

    [Test]
    public void MiddlewareIsNotReturnedForNonMatchingType()
    {
        var map = CreateMiddlewareMap();
        var middleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        map.Add<SimpleMessage>("queue", middleware);

        var handler = map.Get("queue", typeof(AnotherSimpleMessage));

        handler.ShouldBeNull();
    }

    [Test]
    public void MultipleMiddlewareForATypeAreNotSupported()
    {
        HandleMessageMiddleware fn1 = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        HandleMessageMiddleware fn2 = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));

        var map = CreateMiddlewareMap();

        map.Add<SimpleMessage>("queue", fn1);
        map.Add<SimpleMessage>("queue", fn2);

        // Last in wins
        map.Get("queue", typeof(SimpleMessage)).ShouldBe(fn2);
    }

    [Test]
    public void MultipleMiddlewareForATypeWithOtherHandlersAreNotSupported()
    {
        HandleMessageMiddleware fn1 = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        HandleMessageMiddleware fn2 = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(false));
        HandleMessageMiddleware fn3 = new DelegateMessageHandlingMiddleware<AnotherSimpleMessage>(m => Task.FromResult(true));

        var map = CreateMiddlewareMap();
        map.Add<SimpleMessage>("queue", fn1);
        map.Add<AnotherSimpleMessage>("queue", fn3);
        map.Add<SimpleMessage>("queue", fn2);

        // Last in wins
        map.Get("queue", typeof(SimpleMessage)).ShouldBe(fn2);
        map.Get("queue", typeof(AnotherSimpleMessage)).ShouldBe(fn3);
    }

    [Test]
    public void MiddlewareIsNotReturnedForAnotherQueue()
    {
        string queue1 = "queue1";
        string queue2 = "queue2";
        var map = CreateMiddlewareMap();

        map.Add<SimpleMessage>(queue1,
            new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));

        var handler = map.Get(queue2, typeof(SimpleMessage));

        handler.ShouldBeNull();
    }

    [Test]
    public void MiddlewareContainsKeyForMatchingQueueOnly()
    {
        string queue1 = "queue1";
        string queue2 = "queue2";
        var map = CreateMiddlewareMap();

        map.Add<SimpleMessage>(queue1,
            new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));

        map.Contains(queue1, typeof(SimpleMessage)).ShouldBeTrue();
        map.Contains(queue2, typeof(AnotherSimpleMessage)).ShouldBeFalse();
    }

    [Test]
    public void MiddlewareHandlerIsReturnedForQueue()
    {
        string queue1 = "queue1";
        string queue2 = "queue2";

        var map = CreateMiddlewareMap();
        HandleMessageMiddleware fn1 = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        map.Add<SimpleMessage>(queue1,fn1);

        HandleMessageMiddleware fn2 = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        map.Add<SimpleMessage>(queue2, fn2);

        var handler1 = map.Get(queue1, typeof(SimpleMessage));
        handler1.ShouldBe(fn1);

        var handler2 = map.Get(queue2, typeof(SimpleMessage));
        handler2.ShouldBe(fn2);
    }

    public interface IOrderEvent;

    public interface IAuditable;

    public class OrderPlaced : SimpleMessage, IOrderEvent, IAuditable;

    [Test]
    public void GetForMessagePrefersTheExactType()
    {
        var map = CreateMiddlewareMap();
        HandleMessageMiddleware baseMiddleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        HandleMessageMiddleware exactMiddleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        map.Add<Message>("queue", baseMiddleware);
        map.Add<SimpleMessage>("queue", exactMiddleware);

        map.GetForMessage("queue", typeof(SimpleMessage)).ShouldBe(exactMiddleware);
    }

    [Test]
    public void GetForMessageFallsBackToTheClosestBaseClass()
    {
        var map = CreateMiddlewareMap();
        HandleMessageMiddleware messageMiddleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        HandleMessageMiddleware simpleMiddleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        map.Add<IOrderEvent>("queue", new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));
        map.Add<Message>("queue", messageMiddleware);
        map.Add<SimpleMessage>("queue", simpleMiddleware);

        map.GetForMessage("queue", typeof(OrderPlaced)).ShouldBe(simpleMiddleware);
    }

    [Test]
    public void GetForMessageFallsBackToAnInterface()
    {
        var map = CreateMiddlewareMap();
        HandleMessageMiddleware middleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
        map.Add<IOrderEvent>("queue", middleware);

        map.GetForMessage("queue", typeof(OrderPlaced)).ShouldBe(middleware);
    }

    [Test]
    public void GetForMessageReturnsNullWhenMoreThanOneInterfaceMatches()
    {
        var map = CreateMiddlewareMap();
        map.Add<IOrderEvent>("queue", new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));
        map.Add<IAuditable>("queue", new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));

        map.GetForMessage("queue", typeof(OrderPlaced)).ShouldBeNull();
    }

    [Test]
    public void GetForMessageOnlyLooksAtTheQueue()
    {
        var map = CreateMiddlewareMap();
        map.Add<Message>("queue1", new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));
        map.Add<IOrderEvent>("queue1", new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));

        map.GetForMessage("queue2", typeof(OrderPlaced)).ShouldBeNull();
    }

    private static MiddlewareMap CreateMiddlewareMap()
    {
        return new MiddlewareMap();
    }
}