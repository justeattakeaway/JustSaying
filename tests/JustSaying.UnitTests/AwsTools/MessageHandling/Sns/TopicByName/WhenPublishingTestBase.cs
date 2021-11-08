using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public abstract class WhenPublishingTestBase : IAsyncLifetime
{
    private protected SnsMessagePublisher SystemUnderTest { get; private set; }

    public IAmazonSimpleNotificationService Sns { get; private set; } = Substitute.For<IAmazonSimpleNotificationService>();

    public virtual async Task InitializeAsync()
    {
        Given();

        SystemUnderTest = await CreateSystemUnderTestAsync();

        await WhenAsync().ConfigureAwait(false);
    }

    public virtual Task DisposeAsync()
    {
        Sns?.Dispose();
        return Task.CompletedTask;
    }

    protected abstract void Given();
    private protected abstract Task<SnsMessagePublisher> CreateSystemUnderTestAsync();

    protected abstract Task WhenAsync();
}