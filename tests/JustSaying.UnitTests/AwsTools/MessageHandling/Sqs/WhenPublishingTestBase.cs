using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

public abstract class WhenPublishingTestBase<T> : IAsyncLifetime where T : class
{
    private protected SqsMessagePublisher<T> SystemUnderTest { get; private set; }
    public IAmazonSQS Sqs { get; private set; } = Substitute.For<IAmazonSQS>();

    public virtual async Task InitializeAsync()
    {
        Given();

        SystemUnderTest = await CreateSystemUnderTestAsync();

        await WhenAsync().ConfigureAwait(false);
    }

    public virtual Task DisposeAsync()
    {
        Sqs?.Dispose();
        return Task.CompletedTask;
    }

    protected abstract void Given();
    private protected abstract Task<SqsMessagePublisher<T>> CreateSystemUnderTestAsync();

    protected abstract Task WhenAsync();
}
