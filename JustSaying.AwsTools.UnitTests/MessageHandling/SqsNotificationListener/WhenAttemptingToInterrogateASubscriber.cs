using System.Linq;
using JustBehave;
using JustSaying.TestingFramework;
using NUnit.Framework;
using Shouldly;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    [Ignore("Not buggy")]
    public class WhenAttemptingToInterrogateASubscriber : BaseQueuePollingTest
    {
        [Then]
        public void SubscriptedMessagesAreAddedToTheInterrogationDetails()
        {
            SystemUnderTest.Subscribers.Count.ShouldBe(1);
            SystemUnderTest.Subscribers.First(x => x.MessageType == typeof (GenericMessage)).ShouldNotBe(null);
        }
    }
}