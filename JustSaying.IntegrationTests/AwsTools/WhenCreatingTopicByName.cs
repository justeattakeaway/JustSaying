using System;
using Amazon;
using Amazon.SimpleNotificationService;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.IntegrationTests
{
    public abstract class WhenCreatingTopicByName : BehaviourTest<SnsTopicByName>
    {
        protected string UniqueName;
        protected SnsTopicByName CreatedTopic;
        protected IAmazonSimpleNotificationService Bus;

        protected override void Given()
        { }

        protected override SnsTopicByName CreateSystemUnderTest()
        {
            Bus = CreateMeABus.DefaultClientFactory().GetSnsClient(RegionEndpoint.EUWest1);
            UniqueName = "test" + DateTime.Now.Ticks;
            CreatedTopic = new SnsTopicByName(UniqueName, Bus , new MessageSerialisationRegister(), new LoggerFactory());
            CreatedTopic.Create();
            return CreatedTopic;
        }

        public override void PostAssertTeardown()
        {
            Bus.DeleteTopic(CreatedTopic.Arn);
            base.PostAssertTeardown();
        }


    }
}
