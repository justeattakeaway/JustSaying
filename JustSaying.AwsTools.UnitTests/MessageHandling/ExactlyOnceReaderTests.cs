using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling
{
#pragma warning disable 618
    public class UnadornedHandlerAsync : IHandlerAsync<GenericMessage>
    {
        public Task<bool> Handle(GenericMessage message)
        {
            return Task.FromResult(true);
        }
    }

    [ExactlyOnce(TimeOut = 42)]
    public class OnceTestHandlerAsync : IHandlerAsync<GenericMessage>
    {
        public Task<bool> Handle(GenericMessage message)
        {
            return Task.FromResult(true);
        }
    }

    [ExactlyOnce(TimeOut = 23)]
    public class OnceTestHandler : IHandler<GenericMessage>
    {
        public bool Handle(GenericMessage message)
        {
            return true;
        }
    }

    [TestFixture]
    public class ExactlyOnceReaderTests
    {
        [Test]
        public void ObjectDoesNotHaveExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof(object));

            Assert.That(reader.Enabled, Is.False);
        }

        [Test]
        public void UnadornedHandler_DoesNotHaveExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof(UnadornedHandlerAsync));

            Assert.That(reader.Enabled, Is.False);
        }

        [Test]
        public void OnceTestHandlerAsync_DoesHaveExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof(OnceTestHandlerAsync));

            Assert.That(reader.Enabled, Is.True);
        }

        [Test]
        public void OnceTestHandlerAsync_HasCorrectTimeout()
        {
            var reader = new ExactlyOnceReader(typeof(OnceTestHandlerAsync));

            Assert.That(reader.GetTimeOut(), Is.EqualTo(42));
        }

        [Test]
        public void OnceTestHandler_DoesHaveExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof(OnceTestHandler));

            Assert.That(reader.Enabled, Is.True);
        }

        [Test]
        public void OnceTestHandler_HasCorrectTimeout()
        {
            var reader = new ExactlyOnceReader(typeof(OnceTestHandler));

            Assert.That(reader.GetTimeOut(), Is.EqualTo(23));
        }
    }
#pragma warning restore 618
}
