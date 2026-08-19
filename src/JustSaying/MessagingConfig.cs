using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.Naming;

namespace JustSaying;

public class MessagingConfig : IMessagingConfig, IPublishBatchConfiguration
{
    public MessagingConfig()
    {
        PublishFailureReAttempts = JustSayingConstants.DefaultPublisherRetryCount;
        PublishFailureBackoff = JustSayingConstants.DefaultPublisherRetryInterval;
        AdditionalSubscriberAccounts = new List<string>();
        MessageSubjectProvider = new NonGenericMessageSubjectProvider();
        TopicNamingConvention = new DefaultNamingConventions();
        QueueNamingConvention = new DefaultNamingConventions();
        DefaultCompressionOptions = new PublishCompressionOptions();
    }

    public int PublishFailureReAttempts { get; set; }
    public TimeSpan PublishFailureBackoff { get; set; }
    public Action<MessageResponse, object> MessageResponseLogger { get; set; }
    public Action<MessageBatchResponse, IReadOnlyCollection<object>> MessageBatchResponseLogger { get; set; }
    public IReadOnlyCollection<string> AdditionalSubscriberAccounts { get; set; }
    public string Region { get; set; }
    public IMessageSubjectProvider MessageSubjectProvider { get; set; }

    private IMessageTypeRegistry _messageTypeRegistry;

    /// <summary>
    /// Gets or sets the registry that maps message types to their logical wire names and back. When
    /// not set, a default registry backed by <see cref="MessageSubjectProvider"/> is created lazily
    /// on first use, by which point the subject provider has been finalised.
    /// </summary>
    public IMessageTypeRegistry MessageTypeRegistry
    {
        get => _messageTypeRegistry ??= new MessageTypeRegistry(MessageSubjectProvider);
        set => _messageTypeRegistry = value;
    }

    private IMessageMetadataProvider _messageMetadataProvider;

    /// <summary>
    /// Gets or sets the provider used to read intrinsic metadata (id, timestamp, deduplication key)
    /// from message payloads. Defaults to a provider that reads <see cref="Message"/> metadata.
    /// </summary>
    public IMessageMetadataProvider MessageMetadataProvider
    {
        get => _messageMetadataProvider ??= DefaultMessageMetadataProvider.Instance;
        set => _messageMetadataProvider = value;
    }
    public ITopicNamingConvention TopicNamingConvention { get; set; }
    public IQueueNamingConvention QueueNamingConvention { get; set; }
    public PublishCompressionOptions DefaultCompressionOptions { get; set; }

    public virtual void Validate()
    {
        if (MessageSubjectProvider == null)
        {
            throw new InvalidOperationException($"Config cannot have a null for the {nameof(MessageSubjectProvider)} property.");
        }
    }
}
