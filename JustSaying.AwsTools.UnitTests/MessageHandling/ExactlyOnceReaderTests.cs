using JustSaying.AwsTools.MessageHandling;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling
{

    [TestFixture]
    public class ExactlyOnceReaderTests
    {
        [Test]
        public void ObjectTypeDoesNotHaveExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof (object));

            Assert.That(reader.Enabled, Is.False);
        }

        [Test]
        public void UnadornedHandlerType_DoesNotHaveExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof(UnadornedHandlerAsync));

            Assert.That(reader.Enabled, Is.False);
        }

        [Test]
        public void OnceTestHandlerAsyncType_HasExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof (OnceTestHandlerAsync));

            Assert.That(reader.Enabled, Is.True);
        }
    }
}
