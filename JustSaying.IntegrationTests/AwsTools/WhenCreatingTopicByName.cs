using System;
using Amazon;
using Amazon.SimpleNotificationService;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;

namespace JustSaying.IntegrationTests.AwsTools
{
    public abstract class WhenCreatingTopicByName : XBehaviourTest<SnsTopicByName>
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
            CreatedTopic = new SnsTopicByName(UniqueName, Bus , new MessageSerialisationRegister(new NonGenericMessageSubjectProvider()), new LoggerFactory(), new NonGenericMessageSubjectProvider());
            CreatedTopic.CreateAsync().GetAwaiter().GetResult();
            return CreatedTopic;
        }

        protected override void PostAssertTeardown()
        {
            Bus.DeleteTopicAsync(CreatedTopic.Arn).Wait();
            base.PostAssertTeardown();
        }


    }
}
