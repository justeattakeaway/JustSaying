using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public abstract class WhenSnsTopicTestBase : IAsyncLifetime
{
    private protected SnsTopicByName SystemUnderTest { get; private set; }

    public IAmazonSimpleNotificationService Sns { get; private set; } = Substitute.For<IAmazonSimpleNotificationService>();

    public virtual async ValueTask InitializeAsync()
    {
        Given();

        SystemUnderTest = await CreateSystemUnderTestAsync();

        await WhenAsync().ConfigureAwait(false);
    }

    public virtual ValueTask DisposeAsync()
    {
        Sns?.Dispose();
        return ValueTask.CompletedTask;
    }

    protected abstract void Given();
    private protected abstract Task<SnsTopicByName> CreateSystemUnderTestAsync();

    protected abstract Task WhenAsync();
}
