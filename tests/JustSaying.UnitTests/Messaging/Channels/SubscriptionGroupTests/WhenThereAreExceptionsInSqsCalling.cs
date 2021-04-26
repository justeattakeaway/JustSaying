using System.Collections.Generic;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenThereAreExceptionsInSqsCalling : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private int _callCount;

        public WhenThereAreExceptionsInSqsCalling(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue("TestQueue", ExceptionOnFirstCall);
            Queues.Add(_queue);

            SerializationRegister.DefaultDeserializedMessage =
                () => throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing");
        }

        private IEnumerable<Message> ExceptionOnFirstCall()
        {
            _callCount++;
            if (_callCount == 1)
            {
                throw new TestException("testing the failure on first call");
            }

            return new List<Message>();
        }

        protected override bool Until()
        {
            return _callCount > 1;
        }

        [Fact]
        public void QueueIsPolledMoreThanOnce()
        {
            _callCount.ShouldBeGreaterThan(1);
        }
    }
}
