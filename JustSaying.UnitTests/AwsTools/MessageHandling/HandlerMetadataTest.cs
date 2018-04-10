using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling
{
    public class HandlerMetadataTests
    {
        [Fact]
        public void UnadornedHandler_DoesNotHaveExactlyOnce()
        {
            var handler = new UnadornedHandlerAsync();
            var reader = HandlerMetadata.ReadExactlyOnce(handler);

            reader.Enabled.ShouldBeFalse();
        }

        [Fact]
        public void OnceTestHandlerAsync_DoesHaveExactlyOnce()
        {
            var handler = new OnceTestHandlerAsync();
            var reader = HandlerMetadata.ReadExactlyOnce(handler);

            reader.Enabled.ShouldBeTrue();
        }

        [Fact]
        public void OnceTestHandlerAsync_HasCorrectTimeout()
        {
            var handler = new OnceTestHandlerAsync();
            var reader = HandlerMetadata.ReadExactlyOnce(handler);

            reader.GetTimeOut().ShouldBe(42);
        }
    }
}
