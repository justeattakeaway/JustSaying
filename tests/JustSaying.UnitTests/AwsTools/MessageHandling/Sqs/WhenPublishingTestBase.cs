using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

public abstract class WhenPublishingTestBase
{
    private protected SqsMessagePublisher SystemUnderTest { get; private set; }
    public IAmazonSQS Sqs { get; private set; } = Substitute.For<IAmazonSQS>();

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
        Sqs?.Dispose();
    }

    protected abstract void Given();
    private protected abstract Task<SqsMessagePublisher> CreateSystemUnderTestAsync();

    internal static OutboundMessageConverter CreateConverter(bool isRawMessage = false)
    {
        return new OutboundMessageConverter(PublishDestinationType.Queue,
            SimpleMessage.Serializer,
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            nameof(SimpleMessage),
            isRawMessage);
    }

    protected abstract Task WhenAsync();
}
