using System.Threading.Tasks;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs
{
    public abstract class WhenPublishingTestBase : IAsyncLifetime
    {
        public SqsPublisher SystemUnderTest { get; private set; }
        public IAmazonSQS Sqs { get; private set; } = Substitute.For<IAmazonSQS>();

        public virtual async Task InitializeAsync()
        {
            Given();

            SystemUnderTest = await CreateSystemUnderTestAsync();

            await WhenAction().ConfigureAwait(false);
        }

        public virtual Task DisposeAsync()
        {
            if (Sqs != null)
            {
                Sqs.Dispose();
                Sqs = null;
            }

            return Task.CompletedTask;
        }

        protected abstract void Given();
        protected abstract Task<SqsPublisher> CreateSystemUnderTestAsync();

       protected abstract Task WhenAction();
    }
}
