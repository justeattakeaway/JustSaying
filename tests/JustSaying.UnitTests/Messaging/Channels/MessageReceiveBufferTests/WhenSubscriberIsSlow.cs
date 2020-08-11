using System;
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
    public class WhenSubscriberIsSlow : BaseMessageReceiveBufferTests
    {
        private int _callCount;
        private Task<int> SubscriberTask;

        public WhenSubscriberIsSlow(ITestOutputHelper testOutputHelper)
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
                await Task.Delay(TimeSpan.FromMilliseconds(100));
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
