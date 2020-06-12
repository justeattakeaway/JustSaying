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
    public class WhenSqsIsSlow : BaseMessageReceiveBufferTests
    {
        private int _callCount = 0;
        private Task<int> SubscriberTask;

        public WhenSqsIsSlow(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            Queue.GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    Interlocked.Increment(ref _callCount);
                    return new[] { new TestMessage() };
                });
        }

        protected override Task WhenAsync()
        {
            SubscriberTask = Messages();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Subscriber_Task_Completes()
        {
            await SubscriberTask;
        }

        [Fact]
        public async Task All_Messages_Are_Processed()
        {
            var messagesHandled = await SubscriberTask;
            messagesHandled.ShouldBe(_callCount);
        }

        protected async Task<int> Messages()
        {
            int messagesProcessed = 0;

            while (true)
            {
                var couldRead = await SystemUnderTest.Reader.WaitToReadAsync();
                if (!couldRead) break;

                while (SystemUnderTest.Reader.TryRead(out var _))
                {
                    messagesProcessed++;
                }
            }

            return messagesProcessed;
        }
    }
}
