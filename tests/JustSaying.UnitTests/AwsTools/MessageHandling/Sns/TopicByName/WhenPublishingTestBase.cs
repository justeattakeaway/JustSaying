using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public abstract class WhenPublishingTestBase
{
    private protected SnsMessagePublisher SystemUnderTest { get; private set; }

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
    private protected abstract Task<SnsMessagePublisher> CreateSystemUnderTestAsync();

    internal static OutboundMessageConverter CreateConverter(IMessageBodySerializer serializer = null, string subject = null)
    {
        return new OutboundMessageConverter(PublishDestinationType.Topic,
            serializer ?? new SystemTextJsonMessageBodySerializer<SimpleMessage>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions),
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            subject ?? nameof(SimpleMessage),
            false);
    }

    protected abstract Task WhenAsync();
}
