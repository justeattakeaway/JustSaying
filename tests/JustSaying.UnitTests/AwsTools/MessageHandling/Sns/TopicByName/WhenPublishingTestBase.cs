using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public abstract class WhenPublishingTestBase : IAsyncLifetime
{
    private protected SnsMessagePublisher SystemUnderTest { get; private set; }

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
