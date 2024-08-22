using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.Messaging;

public interface IMessageConverter
{
    ReceivedMessage ConvertForReceive(Amazon.SQS.Model.Message message);
    MessageForPublishing ConvertForPublish(Message message, PublishMetadata publishMetadata, PublishDestinationType destinationType);
}
