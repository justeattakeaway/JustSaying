using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.MessageHandling;
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
        public void EmptyMapDoesNotContainKey()
        {
            var map = CreateHandlerMap();
            map.ContainsKey(typeof(SimpleMessage)).ShouldBeFalse();
        }

        [Fact]
        public void EmptyMapReturnsNullHandlers()
        {
            var map = CreateHandlerMap();

            var handler = map.Get(typeof(SimpleMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void HandlerIsReturnedForMatchingType()
        {
            var map = CreateHandlerMap();
            map.Add(typeof(SimpleMessage), m => Task.FromResult(true));

            var handler = map.Get(typeof(SimpleMessage));

            handler.ShouldNotBeNull();
        }

        [Fact]
        public void HandlerContainsKeyForMatchingTypeOnly()
        {
            var map = CreateHandlerMap();
            map.Add(typeof(SimpleMessage), m => Task.FromResult(true));

            map.ContainsKey(typeof(SimpleMessage)).ShouldBeTrue();
            map.ContainsKey(typeof(AnotherSimpleMessage)).ShouldBeFalse();
        }

        [Fact]
        public void HandlerIsNotReturnedForNonMatchingType()
        {
            var map = CreateHandlerMap();
            map.Add(typeof(SimpleMessage), m => Task.FromResult(true));

            var handler = map.Get(typeof(AnotherSimpleMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void CorrectHandlerIsReturnedForType()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = CreateHandlerMap();
            map.Add(typeof(SimpleMessage), fn1);
            map.Add(typeof(AnotherSimpleMessage), fn2);

            var handler1 = map.Get(typeof(SimpleMessage));

            handler1.ShouldBe(fn1);

            var handler2 = map.Get(typeof(AnotherSimpleMessage));

            handler2.ShouldBe(fn2);
        }

        [Fact]
        public void MultipleHandlersForATypeAreNotSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = CreateHandlerMap();

            map.Add(typeof(SimpleMessage), fn1);
            Assert.Throws<InvalidOperationException>(() => map.Add(typeof(SimpleMessage), fn2));
        }

        [Fact]
        public void MultipleHandlersForATypeWithOtherHandlersAreNotSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(false);
            Func<Message, Task<bool>> fn3 = m => Task.FromResult(true);

            var map = CreateHandlerMap();
            map.Add(typeof(SimpleMessage), fn1);
            map.Add(typeof(AnotherSimpleMessage), fn3);

            Assert.Throws<InvalidOperationException>(() => map.Add(typeof(SimpleMessage), fn2));
        }

        private static HandlerMap CreateHandlerMap()
        {
            var monitor = Substitute.For<IMessageMonitor>();

            return new HandlerMap(monitor, NullLoggerFactory.Instance);
        }
    }
}
