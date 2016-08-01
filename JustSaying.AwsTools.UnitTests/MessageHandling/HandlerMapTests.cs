using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling
{
    [TestFixture]
    public class HandlerMapTests
    {
        [Test]
        public void EmptyMapDoesNotContainKey()
        {
            var map = new HandlerMap();
            Assert.That(map.ContainsKey(typeof(GenericMessage)), Is.False);
        }

        [Test]
        public void EmptyMapReturnsNullHandlers()
        {
            var map = new HandlerMap();

            var handler = map.Get(typeof (GenericMessage));

            Assert.That(handler, Is.Null);
        }

        [Test]
        public void HandlerIsReturnedForMatchingType()
        {
            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), m => Task.FromResult(true) );

            var handler = map.Get(typeof(GenericMessage));

            Assert.That(handler, Is.Not.Null);
        }

        [Test]
        public void HandlerContainsKeyForMatchingTypeOnly()
        {
            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), m => Task.FromResult(true));

            Assert.That(map.ContainsKey(typeof(GenericMessage)), Is.True);
            Assert.That(map.ContainsKey(typeof(AnotherGenericMessage)), Is.False);
        }

        [Test]
        public void HandlerIsNotReturnedForNonMatchingType()
        {
            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), m => Task.FromResult(true));

            var handler = map.Get(typeof(AnotherGenericMessage));

            Assert.That(handler, Is.Null);
        }

        [Test]
        public void CorrectHandlerIsReturnedForType()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), fn1);
            map.Add(typeof(AnotherGenericMessage), fn2);

            var handler1 = map.Get(typeof(GenericMessage));

            Assert.That(handler1, Is.Not.Null);
            Assert.That(handler1, Is.EqualTo(fn1));

            var handler2 = map.Get(typeof(AnotherGenericMessage));

            Assert.That(handler2, Is.Not.Null);
            Assert.That(handler2, Is.EqualTo(fn2));
        }

        [Test]
        public void MultipleHandlersForATypeAreNotSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = new HandlerMap();

            map.Add(typeof(GenericMessage), fn1);
            Assert.Throws<ArgumentException>(() => map.Add(typeof(GenericMessage), fn2));
        }

        [Test]
        public void MultipleHandlersForATypeWithOtherHandlersAreNotSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(false);
            Func<Message, Task<bool>> fn3 = m => Task.FromResult(true);

            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), fn1);
            map.Add(typeof(AnotherGenericMessage), fn3);
            Assert.Throws<ArgumentException>(() => map.Add(typeof(GenericMessage), fn2));
        }
    }
}
