using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NUnit.Framework;
using Shouldly;
using Xunit;

namespace JustSaying.AwsTools.UnitTests.MessageHandling
{
    public class HandlerMapTests
    {
        [Fact]
        public void EmptyMapDoesNotContainKey()
        {
            var map = new HandlerMap();
            map.ContainsKey(typeof(GenericMessage)).ShouldBeFalse();
        }

        [Fact]
        public void EmptyMapReturnsNullHandlers()
        {
            var map = new HandlerMap();

            var handler = map.Get(typeof (GenericMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void HandlerIsReturnedForMatchingType()
        {
            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), m => Task.FromResult(true) );

            var handler = map.Get(typeof(GenericMessage));

            handler.ShouldNotBeNull();
        }

        [Fact]
        public void HandlerContainsKeyForMatchingTypeOnly()
        {
            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), m => Task.FromResult(true));

            map.ContainsKey(typeof(GenericMessage)).ShouldBeTrue();
            map.ContainsKey(typeof(AnotherGenericMessage)).ShouldBeFalse();
        }

        [Fact]
        public void HandlerIsNotReturnedForNonMatchingType()
        {
            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), m => Task.FromResult(true));

            var handler = map.Get(typeof(AnotherGenericMessage));

            handler.ShouldBeNull();
        }

        [Fact]
        public void CorrectHandlerIsReturnedForType()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), fn1);
            map.Add(typeof(AnotherGenericMessage), fn2);

            var handler1 = map.Get(typeof(GenericMessage));

            handler1.ShouldBe(fn1);
            
            var handler2 = map.Get(typeof(AnotherGenericMessage));

            handler2.ShouldBe(fn2);
        }

        [Fact]
        public void MultipleHandlersForATypeAreNotSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = new HandlerMap();

            map.Add(typeof(GenericMessage), fn1);
            new Action(() => map.Add(typeof(GenericMessage), fn2)).ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void MultipleHandlersForATypeWithOtherHandlersAreNotSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(false);
            Func<Message, Task<bool>> fn3 = m => Task.FromResult(true);

            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), fn1);
            map.Add(typeof(AnotherGenericMessage), fn3);
            new Action(() => map.Add(typeof(GenericMessage), fn2)).ShouldThrow<ArgumentException>();
        }
    }
}
