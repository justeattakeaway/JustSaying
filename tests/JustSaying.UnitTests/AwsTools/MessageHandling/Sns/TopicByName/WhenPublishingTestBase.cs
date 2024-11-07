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

    internal static PublishMessageConverter CreateConverter(IMessageBodySerializer serializer = null, string subject = null)
    {
        return new PublishMessageConverter(PublishDestinationType.Topic,
            serializer ?? new SystemTextJsonMessageBodySerializer<SimpleMessage>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions),
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            subject ?? nameof(SimpleMessage),
            false);
    }

    protected abstract Task WhenAsync();
}
