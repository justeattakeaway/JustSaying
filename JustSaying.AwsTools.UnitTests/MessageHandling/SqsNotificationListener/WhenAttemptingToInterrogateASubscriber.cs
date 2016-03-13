using System.Linq;
using System.Threading.Tasks;
using JustBehave;
using JustSaying.TestingFramework;
using Shouldly;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenAttemptingToInterrogateASubscriber : BaseQueuePollingTest
    {
        protected override async Task When()
        {
            await base.When();

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