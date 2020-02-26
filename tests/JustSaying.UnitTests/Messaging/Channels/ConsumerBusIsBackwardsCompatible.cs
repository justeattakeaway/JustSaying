using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels
{
    public class ConsumerBusIsBackwardsCompatible
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ConsumerBusIsBackwardsCompatible(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task No_Message_To_Process_Queue_Keeps_Running()
        {
            int requested = 0;
            var sqsQueue = TestQueue(() => GetMessages(() => Interlocked.Increment(ref requested)));

            var queues = new List<ISqsQueue> { sqsQueue };
            IMessageDispatcher dispatcher = TestDispatcher();
            var bus = new ConsumerBus(queues, 1, dispatcher, _testOutputHelper.ToLoggerFactory());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            bus.Start(cts.Token);

            await bus.Completion;

            requested.ShouldBeGreaterThan(2);
        }

        private static async Task<IList<Message>> GetMessages(Action spy = null)
        {
            await Task.Delay(5).ConfigureAwait(false);
            spy?.Invoke();
            return new List<Message>();
        }

        private static Task<IList<Message>> GetErrorMessages()
        {
            throw new InvalidOperationException();
        }

        private static ISqsQueue TestQueue(Func<Task<IList<Message>>> getMessages)
        {
            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock
                .GetMessages(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await getMessages());

            return sqsQueueMock;
        }

        private static IMessageDispatcher TestDispatcher(Action spy = null)
        {
            async Task OnDispatchMessage()
            {
                await Task.Delay(5).ConfigureAwait(false);
                spy?.Invoke();
            }

            IMessageDispatcher dispatcherMock = Substitute.For<IMessageDispatcher>();
            dispatcherMock
                .DispatchMessage(Arg.Any<IQueueMessageContext>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await OnDispatchMessage());

            return dispatcherMock;
        }
    }
}
