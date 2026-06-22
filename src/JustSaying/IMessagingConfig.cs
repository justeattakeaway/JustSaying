using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Naming;

namespace JustSaying;

public interface IMessagingConfig : IPublishConfiguration
{
    string Region { get; set; }
    IMessageSubjectProvider MessageSubjectProvider { get; set; }
    IMessageMetadataProvider MessageMetadataProvider { get; set; }
    ITopicNamingConvention TopicNamingConvention { get; set; }
    IQueueNamingConvention QueueNamingConvention { get; set; }
    PublishCompressionOptions DefaultCompressionOptions { get; set; }
    void Validate();
}
