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
