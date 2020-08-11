using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests
{
    public class WhenThereAreNoSubscribers : BaseMessageReceiveBufferTests
    {
        private int _callCount;

        public WhenThereAreNoSubscribers(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            SqsClient.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    Interlocked.Increment(ref _callCount);
                    var messages = new List<Message> { new TestMessage() };
                    return new ReceiveMessageResponse { Messages = messages };
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
