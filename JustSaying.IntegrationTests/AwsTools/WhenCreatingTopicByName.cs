using System.Threading.Tasks;
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
    public abstract class WhenCreatingTopicByName : XAsyncBehaviourTest<SnsTopicByName>
    {
        protected string UniqueName => TestFixture.UniqueName;

        protected SnsTopicByName CreatedTopic { get; private set; }

        protected IAmazonSimpleNotificationService Client { get; private set; }

        protected ILoggerFactory LoggerFactory => TestFixture.LoggerFactory;

        protected RegionEndpoint Region => TestFixture.Region;

        private JustSayingFixture TestFixture { get; } = new JustSayingFixture();

        protected override void Given()
        {
        }

        protected override SnsTopicByName CreateSystemUnderTest()
        {
            Client = TestFixture.CreateSnsClient();

            CreatedTopic = new SnsTopicByName(
                UniqueName,
                Client,
                new MessageSerialisationRegister(new NonGenericMessageSubjectProvider()),
                LoggerFactory,
                new NonGenericMessageSubjectProvider());

            CreatedTopic.CreateAsync().ResultSync();

            return CreatedTopic;
        }

        protected override void PostAssertTeardown()
        {
            Client.DeleteTopicAsync(CreatedTopic.Arn).ResultSync();
            base.PostAssertTeardown();
        }
    }
}
