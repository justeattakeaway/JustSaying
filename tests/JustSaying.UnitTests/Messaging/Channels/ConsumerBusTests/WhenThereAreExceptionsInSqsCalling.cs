using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests
{
    public class WhenThereAreExceptionsInSqsCalling : BaseConsumerBusTests
    {
        private ISqsQueue _queue;
        private int _callCount;

        public WhenThereAreExceptionsInSqsCalling(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = Substitute.For<ISqsQueue>();
            _queue.GetMessages(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ => ExceptionOnFirstCall());
            _queue.Uri.Returns(new Uri("http://foo.com"));

            Queues.Add(_queue);
            Handler.Handle(null).ReturnsForAnyArgs(true);

            SerializationRegister
                .DeserializeMessage(Arg.Any<string>())
                .Returns(x => throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing"));
        }

        private Task ExceptionOnFirstCall()
        {
            _callCount++;
            if (_callCount == 1)
            {
                throw new TestException("testing the failure on first call");
            }
            
            return Task.FromResult(new ReceiveMessageResponse());
        }

        protected override async Task WhenAsync()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            SystemUnderTest.Start(cts.Token);

            await SystemUnderTest.Completion;
        }

        // todo: this one fails because we haven't handled this error yet
        [Fact]
        public void QueueIsPolledMoreThanOnce()
        {
            _callCount.ShouldBeGreaterThan(1);
        }
    }
}
