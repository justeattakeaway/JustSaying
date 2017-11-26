using JustSaying.AwsTools.MessageHandling;
using Shouldly;
using Xunit;

namespace JustSaying.AwsTools.UnitTests.MessageHandling
{
    public class ExactlyOnceReaderTests
    {
        [Fact]
        public void ObjectTypeDoesNotHaveExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof (object));

            reader.Enabled.ShouldBeFalse();
        }

        [Fact]
        public void UnadornedHandlerType_DoesNotHaveExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof(UnadornedHandlerAsync));

            reader.Enabled.ShouldBeFalse();
        }

        [Fact]
        public void OnceTestHandlerAsyncType_HasExactlyOnce()
        {
            var reader = new ExactlyOnceReader(typeof (OnceTestHandlerAsync));

            reader.Enabled.ShouldBeTrue();
        }
    }
}
