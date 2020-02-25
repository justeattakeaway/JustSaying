using System;
using System.Collections.Generic;
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
    public class ErrorHandlingTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public async Task Sqs_Client_Throwing_Exceptions_Continues_To_Request_Messages()
        {
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(GetErrorMessages);

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher = TestDispatcher(() => Interlocked.Increment(ref messagesDispatched));
            var bus = new ConsumerBus(queues, 1, dispatcher, _testOutputHelper.ToLoggerFactory());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            bus.Start(cts.Token);

            await bus.Completion;

            messagesDispatched.ShouldBe(0);
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
