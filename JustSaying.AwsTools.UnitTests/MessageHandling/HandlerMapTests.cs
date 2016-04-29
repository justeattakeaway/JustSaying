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
        public void EmptyMapReturnsNullHandlers()
        {
            var map = new HandlerMap();

            var handlers = map.Get(typeof (GenericMessage));

            Assert.That(handlers, Is.Null);
        }

        [Test]
        public void HandlerIsReturnedForMatchingType()
        {
            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), m => Task.FromResult(true) );

            var handlers = map.Get(typeof(GenericMessage));

            Assert.That(handlers, Is.Not.Null);
            Assert.That(handlers.Count, Is.EqualTo(1));
        }

        [Test]
        public void HandlerIsNotReturnedForNonMatchingType()
        {
            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), m => Task.FromResult(true));

            var handlers = map.Get(typeof(AnotherGenericMessage));

            Assert.That(handlers, Is.Null);
        }

        [Test]
        public void CorrectHandlerIsReturnedForType()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), fn1);
            map.Add(typeof(AnotherGenericMessage), fn2);

            var handlers1 = map.Get(typeof(GenericMessage));

            Assert.That(handlers1, Is.Not.Null);
            Assert.That(handlers1.Count, Is.EqualTo(1));
            Assert.That(handlers1[0], Is.EqualTo(fn1));

            var handlers2 = map.Get(typeof(AnotherGenericMessage));

            Assert.That(handlers2, Is.Not.Null);
            Assert.That(handlers2.Count, Is.EqualTo(1));
            Assert.That(handlers2[0], Is.EqualTo(fn2));
        }

        [Test]
        public void MultipleHandlersForATypeAreSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(true);

            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), fn1);
            map.Add(typeof(GenericMessage), fn2);

            var handlers1 = map.Get(typeof(GenericMessage));

            Assert.That(handlers1, Is.Not.Null);
            Assert.That(handlers1.Count, Is.EqualTo(2));
            Assert.That(handlers1[0], Is.EqualTo(fn1));
            Assert.That(handlers1[1], Is.EqualTo(fn2));

            var handlers2 = map.Get(typeof(AnotherGenericMessage));
            Assert.That(handlers2, Is.Null);
        }

        [Test]
        public void MultipleHandlersForATypeWithOtherHandlersAreSupported()
        {
            Func<Message, Task<bool>> fn1 = m => Task.FromResult(true);
            Func<Message, Task<bool>> fn2 = m => Task.FromResult(false);
            Func<Message, Task<bool>> fn3 = m => Task.FromResult(true);

            var map = new HandlerMap();
            map.Add(typeof(GenericMessage), fn1);
            map.Add(typeof(GenericMessage), fn2);
            map.Add(typeof(AnotherGenericMessage), fn3);

            var handlers1 = map.Get(typeof(GenericMessage));

            Assert.That(handlers1, Is.Not.Null);
            Assert.That(handlers1.Count, Is.EqualTo(2));
            Assert.That(handlers1[0], Is.EqualTo(fn1));
            Assert.That(handlers1[1], Is.EqualTo(fn2));

            var handlers2 = map.Get(typeof(AnotherGenericMessage));
            Assert.That(handlers2, Is.Not.Null);
            Assert.That(handlers2.Count, Is.EqualTo(1));
        }
    }
}
