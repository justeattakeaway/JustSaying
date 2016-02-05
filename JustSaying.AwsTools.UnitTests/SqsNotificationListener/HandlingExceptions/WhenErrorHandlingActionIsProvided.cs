using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustBehave;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using NUnit.Framework;
using JustSaying.TestingFramework;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener.HandlingExceptions
{
    public class WhenErrorHandlingActionIsProvided : BaseQueuePollingTest
    {
        private Action<Exception, Amazon.SQS.Model.Message> _globalErrorHandler;
        private bool _handledException;

        protected override JustSaying.AwsTools.MessageHandling.SqsNotificationListener CreateSystemUnderTest()
        {
            _globalErrorHandler = (ex, m) => { _handledException = true; };

            return new JustSaying.AwsTools.MessageHandling.SqsNotificationListener(new SqsQueueByUrl(RegionEndpoint.EUWest1, QueueUrl, Sqs), SerialisationRegister, Monitor, onError: _globalErrorHandler);
        }

        protected override void When()
        {
            SystemUnderTest.HandleMessage(null);
        }

        [Then]
        public void NoExceptionIsThrown()
        {
            Assert.That(ThrownException, Is.Null);
        }

        [Then]
        public async Task CustomExceptionHandlingIsCalled()
        {
            await Patiently.AssertThatAsync(() => _handledException);
        }

        protected override void Given()
        {
            Sqs = Substitute.For<IAmazonSQS>();
        }
    }
}