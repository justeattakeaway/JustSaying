using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.Handle.HandleMessageContext, bool>;

namespace JustSaying.UnitTests.Messaging.Middleware
{
    public class MiddlewareMapTests
    {
        [Fact]
        public void EmptyMapDoesNotContain()
        {
            var map = CreateMiddlewareMap();
            map.Contains("queue", typeof(SimpleMessage)).ShouldBeFalse();
        }

        [Fact]
        public void EmptyMapReturnsNullMiddleware()
        {
            var map = CreateMiddlewareMap();

            var handler = map.Get("queue", typeof(SimpleMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void MiddlewareIsReturnedForMatchingType()
        {
            var map = CreateMiddlewareMap();

            var middleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
            map.Add<SimpleMessage>("queue",  () => middleware);

            var handler = map.Get("queue", typeof(SimpleMessage));

            handler.ShouldNotBeNull();
        }

        [Fact]
        public void MiddlewareContainsKeyForMatchingTypeOnly()
        {
            var map = CreateMiddlewareMap();
            var middleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
            map.Add<SimpleMessage>("queue", () => middleware);

            map.Contains("queue", typeof(SimpleMessage)).ShouldBeTrue();
            map.Contains("queue", typeof(AnotherSimpleMessage)).ShouldBeFalse();
        }

        [Fact]
        public void MiddlewareIsNotReturnedForNonMatchingType()
        {
            var map = CreateMiddlewareMap();
            var middleware = new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
            map.Add<SimpleMessage>("queue", () => middleware);

            var handler = map.Get("queue", typeof(AnotherSimpleMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void MultipleMiddlewareForATypeAreNotSupported()
        {
            Func<HandleMessageMiddleware> fn1 = () => new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
            Func<HandleMessageMiddleware> fn2 = () => new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));

            var map = CreateMiddlewareMap();

            map.Add<SimpleMessage>("queue", fn1);
            map.Add<SimpleMessage>("queue", fn2);

            // Last in wins
            map.Get("queue", typeof(SimpleMessage)).ShouldBe(fn2);
        }

        [Fact]
        public void MultipleMiddlewareForATypeWithOtherHandlersAreNotSupported()
        {
            Func<HandleMessageMiddleware> fn1 = () =>  new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
            Func<HandleMessageMiddleware> fn2 = () =>  new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(false));
            Func<HandleMessageMiddleware> fn3 = () =>  new DelegateMessageHandlingMiddleware<AnotherSimpleMessage>(m => Task.FromResult(true));

            var map = CreateMiddlewareMap();
            map.Add<SimpleMessage>("queue", fn1);
            map.Add<AnotherSimpleMessage>("queue", fn3);
            map.Add<SimpleMessage>("queue", fn2);

            // Last in wins
            map.Get("queue", typeof(SimpleMessage)).ShouldBe(fn2);
            map.Get("queue", typeof(AnotherSimpleMessage)).ShouldBe(fn3);
        }

        [Fact]
        public void MiddlewareIsNotReturnedForAnotherQueue()
        {
            string queue1 = "queue1";
            string queue2 = "queue2";
            var map = CreateMiddlewareMap();

            map.Add<SimpleMessage>(queue1,
                () => new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));

            var handler = map.Get(queue2, typeof(SimpleMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void MiddlewareContainsKeyForMatchingQueueOnly()
        {
            string queue1 = "queue1";
            string queue2 = "queue2";
            var map = CreateMiddlewareMap();

            map.Add<SimpleMessage>(queue1,
                () => new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true)));

            map.Contains(queue1, typeof(SimpleMessage)).ShouldBeTrue();
            map.Contains(queue2, typeof(AnotherSimpleMessage)).ShouldBeFalse();
        }

        [Fact]
        public void MiddlewareHandlerIsReturnedForQueue()
        {
            string queue1 = "queue1";
            string queue2 = "queue2";

            var map = CreateMiddlewareMap();
            Func<HandleMessageMiddleware> fn1 = () => new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
            map.Add<SimpleMessage>(queue1,fn1);

            Func<HandleMessageMiddleware> fn2 = () =>  new DelegateMessageHandlingMiddleware<SimpleMessage>(m => Task.FromResult(true));
            map.Add<SimpleMessage>(queue2, fn2);

            var handler1 = map.Get(queue1, typeof(SimpleMessage));
            handler1.ShouldBe(fn1);

            var handler2 = map.Get(queue2, typeof(SimpleMessage));
            handler2.ShouldBe(fn2);
        }

        private static MiddlewareMap CreateMiddlewareMap()
        {
            var monitor = Substitute.For<IMessageMonitor>();

            return new MiddlewareMap(monitor, NullLoggerFactory.Instance);
        }
    }
}
