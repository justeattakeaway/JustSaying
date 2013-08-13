using System;
using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenThereAreExceptionsInSqsCalling : BaseQueuePollingTest
    {
        private int _sqsCallCounter;

        protected override void Given()
        {
            TestWaitTime = 50;
            base.Given();
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>())
                .Returns(x => { throw new Exception(); })
                .AndDoes(x => { _sqsCallCounter++; });
        }

        [Then]
        public void QueueIsPolledMoreThanOnce()
        {
            Assert.Greater(_sqsCallCounter, 1);
        }
    }
}