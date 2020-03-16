using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests
{
    public class WhenConsumerIsSlow : BaseMessageReceiveBufferTests
    {
        private int _callCount = 0;
        private Task<int> ConsumerTask;

        public WhenConsumerIsSlow(ITestOutputHelper testOutputHelper)
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

        protected override Task WhenAsync()
        {
            ConsumerTask = Messages();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Consumer_Task_Completes()
        {
            await ConsumerTask;
        }

        [Fact]
        public async Task All_Messages_Are_Processed()
        {
            var messagesHandled = await ConsumerTask;
            messagesHandled.ShouldBe(_callCount);
        }

        protected async Task<int> Messages()
        {
            int messagesProcessed = 0;

            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                var couldWait = await SystemUnderTest.Reader.WaitToReadAsync();
                if (!couldWait) break;

                while (SystemUnderTest.Reader.TryRead(out var _))
                {
                    messagesProcessed++;
                }
            }

            return messagesProcessed;
        }
    }
}