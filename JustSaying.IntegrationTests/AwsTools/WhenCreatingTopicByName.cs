using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
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

        protected override Task Given() => Task.CompletedTask;

        protected override async Task<SnsTopicByName> CreateSystemUnderTestAsync()
        {
            Client = TestFixture.CreateSnsClient();

            CreatedTopic = new SnsTopicByName(
                UniqueName,
                Client,
                new MessageSerializationRegister(new NonGenericMessageSubjectProvider()),
                LoggerFactory,
                new NonGenericMessageSubjectProvider());

            await CreatedTopic.CreateAsync();

            return CreatedTopic;
        }

        protected override Task PostAssertTeardownAsync()
        {
            return Client.DeleteTopicAsync(CreatedTopic.Arn);
        }
    }
}
