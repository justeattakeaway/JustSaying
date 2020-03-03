using JustSaying.Messaging.Channels;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Channels.PolicyBuilderTests
{
    public class CanCreatePolicy
    {
        [Fact]
        public void PolicyBuilder()
        {
            bool called = false;
            var noop = SqsPolicyBuilder.Build<int>();

            var result = noop.Run(() =>
            {
                called = true;
                return 1;
            });

            called.ShouldBeTrue();
            result.ShouldBe(1);
        }

        [Fact]
        public void PolicyBuilder_Error_Handler()
        {
            bool called = false;
            var noop = SqsPolicyBuilder.Build<int>(
                next => new ErrorHandlingSqsPolicy<int>(next));

            var result = noop.Run(() =>
            {
                called = true;
                throw new System.InvalidOperationException();
            });

            called.ShouldBeTrue();
            result.ShouldBe(0);
        }
    }
}
