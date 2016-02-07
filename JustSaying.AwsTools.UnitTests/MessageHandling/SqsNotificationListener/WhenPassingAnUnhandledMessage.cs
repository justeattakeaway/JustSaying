using System.Collections.Generic;
using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenPassingAnUnhandledMessage : BaseQueuePollingTest
    {
        protected override void Given()
        {
            TestWaitTime = 100;
            base.Given();
            SerialisationRegister.GetSerialiser(Arg.Any<string>()).Returns(x => { throw new KeyNotFoundException(); });
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            Sqs.Received().DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }
    }
}