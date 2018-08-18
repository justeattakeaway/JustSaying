using System;
using Amazon;
using Amazon.SimpleNotificationService;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public abstract class WhenCreatingTopicByName : XBehaviourTest<SnsTopicByName>
    {
        protected string UniqueName { get; private set; }

        protected SnsTopicByName CreatedTopic { get; private set; }

        protected IAmazonSimpleNotificationService Client { get; private set; }

        protected ILoggerFactory LoggerFactory { get; private set; }

        protected RegionEndpoint Region => TestEnvironment.Region;

        protected override void Given()
        {
        }

        protected override SnsTopicByName CreateSystemUnderTest()
        {
            Client = CreateMeABus.DefaultClientFactory().GetSnsClient(Region);
            LoggerFactory = new LoggerFactory();
            UniqueName = "test" + DateTime.Now.Ticks;

            CreatedTopic = new SnsTopicByName(
                UniqueName,
                Client,
                new MessageSerialisationRegister(new NonGenericMessageSubjectProvider()),
                LoggerFactory,
                new NonGenericMessageSubjectProvider());

            CreatedTopic.CreateAsync().GetAwaiter().GetResult();

            return CreatedTopic;
        }

        protected override void PostAssertTeardown()
        {
            Client.DeleteTopicAsync(CreatedTopic.Arn).Wait();
            base.PostAssertTeardown();
        }
    }
}
