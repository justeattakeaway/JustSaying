using System.Collections.Generic;
using System.Threading;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests
{
    public class WhenThereAreNoConsumers : BaseMessageReceiveBufferTests
    {
        private int _callCount = 0;

        public WhenThereAreNoConsumers(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            Queue.GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    Interlocked.Increment(ref _callCount);
                    return new[] { new TestMessage() };
                });
        }

        [Fact]
        public void Queue_Is_Filled()
        {
            _callCount.ShouldBeGreaterThan(0);
        }
    }
}
