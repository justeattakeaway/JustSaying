using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling
{
    [TestFixture]
    public class HandlerMetadataTests
    {
        [Test]
        public void UnadornedHandler_DoesNotHaveExactlyOnce()
        {
            var handler = new UnadornedHandlerAsync();
            var reader = HandlerMetadata.ReadExactlyOnce(handler);

            Assert.That(reader.Enabled, Is.False);
        }

        [Test]
        public void OnceTestHandlerAsync_DoesHaveExactlyOnce()
        {
            var handler = new OnceTestHandlerAsync();
            var reader = HandlerMetadata.ReadExactlyOnce(handler);

            Assert.That(reader.Enabled, Is.True);
        }

        [Test]
        public void OnceTestHandlerAsync_HasCorrectTimeout()
        {
            var handler = new OnceTestHandlerAsync();
            var reader = HandlerMetadata.ReadExactlyOnce(handler);

            Assert.That(reader.GetTimeOut(), Is.EqualTo(42));
        }

        [Test]
        public void OnceTestHandler_DoesHaveExactlyOnce()
        {
            var handler = new OnceTestHandler();
            var reader = HandlerMetadata.ReadExactlyOnce(handler);

            Assert.That(reader.Enabled, Is.True);
        }

        [Test]
        public void OnceTestHandler_HasCorrectTimeout()
        {
            var handler = new OnceTestHandler();
            var reader = HandlerMetadata.ReadExactlyOnce(handler);

            Assert.That(reader.GetTimeOut(), Is.EqualTo(23));
        }

        [Test]
        public void WrappedHandler_DoesHaveExactlyOnce()
        {
            var wrapped = new BlockingHandler<GenericMessage>(new OnceTestHandler());

            var reader = HandlerMetadata.ReadExactlyOnce(wrapped);

            Assert.That(reader.Enabled, Is.True);
        }
    }
#pragma warning restore 618
}
