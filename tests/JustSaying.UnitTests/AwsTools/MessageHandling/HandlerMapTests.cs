using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling
{
    public class HandlerMapTests
    {
        [Fact]
        public void EmptyMapDoesNotContain()
        {
            var map = CreateHandlerMap();
            map.Contains("queue", typeof(SimpleMessage)).ShouldBeFalse();
        }

        [Fact]
        public void EmptyMapReturnsNullHandlers()
        {
            var map = CreateHandlerMap();

            var handler = map.Get("queue", typeof(SimpleMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void HandlerIsReturnedForMatchingType()
        {
            var map = CreateHandlerMap();
            map.Add("queue", typeof(SimpleMessage), m => Task.FromResult(true));

            var handler = map.Get("queue", typeof(SimpleMessage));

            handler.ShouldNotBeNull();
        }

        [Fact]
        public void HandlerContainsKeyForMatchingTypeOnly()
        {
            var map = CreateHandlerMap();
            map.Add("queue", typeof(SimpleMessage), m => Task.FromResult(true));

            map.Contains("queue", typeof(SimpleMessage)).ShouldBeTrue();
            map.Contains("queue", typeof(AnotherSimpleMessage)).ShouldBeFalse();
        }

        [Fact]
        public void HandlerIsNotReturnedForNonMatchingType()
        {
            var map = CreateHandlerMap();
            map.Add("queue", typeof(SimpleMessage), m => Task.FromResult(true));

            var handler = map.Get("queue", typeof(AnotherSimpleMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void CorrectHandlerIsReturnedForType()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = CreateHandlerMap();
            map.Add("queue", typeof(SimpleMessage), fn1);
            map.Add("queue", typeof(AnotherSimpleMessage), fn2);

            var handler1 = map.Get("queue", typeof(SimpleMessage));

            handler1.ShouldBe(fn1);

            var handler2 = map.Get("queue", typeof(AnotherSimpleMessage));

            handler2.ShouldBe(fn2);
        }

        [Fact]
        public void MultipleHandlersForATypeAreNotSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = CreateHandlerMap();

            map.Add("queue", typeof(SimpleMessage), fn1);
            map.Add("queue", typeof(SimpleMessage), fn2);

            // Last in wins
            map.Get("queue", typeof(SimpleMessage)).ShouldBe(fn2);
        }

        [Fact]
        public void MultipleHandlersForATypeWithOtherHandlersAreNotSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(false);
            Func<Message, Task<bool>> fn3 = m => Task.FromResult(true);

            var map = CreateHandlerMap();
            map.Add("queue", typeof(SimpleMessage), fn1);
            map.Add("queue", typeof(AnotherSimpleMessage), fn3);
            map.Add("queue", typeof(SimpleMessage), fn2);

            // Last in wins
            map.Get("queue", typeof(SimpleMessage)).ShouldBe(fn2);
            map.Get("queue", typeof(AnotherSimpleMessage)).ShouldBe(fn3);
        }

        [Fact]
        public void HandlerIsNotReturnedForAnotherQueue()
        {
            string queue1 = "queue1";
            string queue2 = "queue2";
            var map = CreateHandlerMap();
            map.Add(queue1, typeof(SimpleMessage), m => Task.FromResult(true));

            var handler = map.Get(queue2, typeof(SimpleMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void HandlerContainsKeyForMatchingQueueOnly()
        {
            string queue1 = "queue1";
            string queue2 = "queue2";
            var map = CreateHandlerMap();
            map.Add(queue1, typeof(SimpleMessage), m => Task.FromResult(true));

            map.Contains(queue1, typeof(SimpleMessage)).ShouldBeTrue();
            map.Contains(queue2, typeof(AnotherSimpleMessage)).ShouldBeFalse();
        }

        [Fact]
        public void CorrectHandlerIsReturnedForQueue()
        {
            string queue1 = "queue1";
            string queue2 = "queue2";
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = CreateHandlerMap();
            map.Add(queue1, typeof(SimpleMessage), fn1);
            map.Add(queue2, typeof(SimpleMessage), fn2);

            var handler1 = map.Get(queue1, typeof(SimpleMessage));

            handler1.ShouldBe(fn1);

            var handler2 = map.Get(queue2, typeof(SimpleMessage));

            handler2.ShouldBe(fn2);
        }

        private static HandlerMap CreateHandlerMap()
        {
            var monitor = Substitute.For<IMessageMonitor>();

            return new HandlerMap(monitor, NullLoggerFactory.Instance);
        }
    }
}
