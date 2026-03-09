using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public abstract class WhenSnsTopicTestBase
{
    private protected SnsTopicByName SystemUnderTest { get; private set; }

    public IAmazonSimpleNotificationService Sns { get; private set; } = Substitute.For<IAmazonSimpleNotificationService>();

    [Before(Test)]
    public virtual async Task SetUp()
    {
        Given();

        SystemUnderTest = await CreateSystemUnderTestAsync();

        await WhenAsync().ConfigureAwait(false);
    }

    [After(Test)]
    public virtual async Task TearDown()
    {
        Sns?.Dispose();
    }

    protected abstract void Given();
    private protected abstract Task<SnsTopicByName> CreateSystemUnderTestAsync();

    protected abstract Task WhenAsync();
}
