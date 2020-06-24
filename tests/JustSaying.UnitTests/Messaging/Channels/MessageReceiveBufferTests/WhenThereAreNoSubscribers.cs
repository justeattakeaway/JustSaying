using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests
{
    public class WhenThereAreNoSubscribers : BaseMessageReceiveBufferTests
    {
        private int _callCount = 0;

        public WhenThereAreNoSubscribers(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            Queue.GetMessagesAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    Interlocked.Increment(ref _callCount);
                    return new[] { new TestMessage() };
                });
        }

        protected override Task WhenAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public void Buffer_Is_Filled()
        {
            _callCount.ShouldBeGreaterThan(0);
        }
    }
}
