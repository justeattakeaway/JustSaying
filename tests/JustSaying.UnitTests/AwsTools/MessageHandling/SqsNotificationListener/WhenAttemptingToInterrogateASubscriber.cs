using System.Linq;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenAttemptingToInterrogateASubscriber : BaseQueuePollingTest
    {
        [Fact]
        public void SubscribedMessagesAreAddedToTheInterrogationDetails()
        {
            SystemUnderTest.Subscribers.Count.ShouldBe(1);
            SystemUnderTest.Subscribers.First(x => x.MessageType == typeof(SimpleMessage)).ShouldNotBe(null);
        }
    }
}
