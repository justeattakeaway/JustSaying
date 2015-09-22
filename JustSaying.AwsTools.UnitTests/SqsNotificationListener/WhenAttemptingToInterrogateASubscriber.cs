using System.Linq;
using AwsTools.UnitTests.SqsNotificationListener;
using JustBehave;
using JustSaying.TestingFramework;
using Shouldly;

namespace JustSaying.AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenAttemptingToInterrogateASubscriber : BaseQueuePollingTest
    {
        protected override void When()
        {
            base.When();

            SystemUnderTest.StopListening();

            SystemUnderTest.Listen();
        }

        [Then]
        public void SubscriptedMessagesAreAddedToTheInterrogationDetails()
        {
            SystemUnderTest.Subscribers.Count.ShouldBe(1);
            SystemUnderTest.Subscribers.First(x => x.MessageType == typeof (GenericMessage)).ShouldNotBe(null);
        }
    }
}