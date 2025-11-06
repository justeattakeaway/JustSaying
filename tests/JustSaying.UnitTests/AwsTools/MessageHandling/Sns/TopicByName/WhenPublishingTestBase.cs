using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;
using Xunit;
#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName
{
    public abstract class WhenPublishingTestBase : IAsyncLifetime
    {
        private protected SnsTopicByName SystemUnderTest { get; private set; }

        public IAmazonSimpleNotificationService Sns { get; private set; } = Substitute.For<IAmazonSimpleNotificationService>();

        public virtual async Task InitializeAsync()
        {
            Given();

            SystemUnderTest = await CreateSystemUnderTestAsync();

            await WhenAsync().ConfigureAwait(false);
        }

        public virtual Task DisposeAsync()
        {
            if (Sns != null)
            {
                Sns.Dispose();
                Sns = null;
            }

            return Task.CompletedTask;
        }

        protected abstract void Given();
        private protected abstract Task<SnsTopicByName> CreateSystemUnderTestAsync();

        protected abstract Task WhenAsync();
    }
}
