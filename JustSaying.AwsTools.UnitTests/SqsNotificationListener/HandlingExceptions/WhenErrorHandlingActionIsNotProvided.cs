using Amazon;
using JustBehave;
using JustSaying.AwsTools;
using NSubstitute;
using NUnit.Framework;

namespace AwsTools.UnitTests.SqsNotificationListener.HandlingExceptions
{
    public class WhenErrorHandlingActionIsNotProvided : BaseQueuePollingTest
    {
        protected override JustSaying.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustSaying.AwsTools.SqsNotificationListener(new SqsQueueByUrl(QueueUrl, Sqs), SerialisationRegister, Monitor);
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

        protected override void Given()
        {
            Sqs = Substitute.For<ISqsClient>();
            Sqs.Region.Returns(RegionEndpoint.EUWest1);
        }
    }
}