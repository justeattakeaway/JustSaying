using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.Messaging;

public interface IPublishMessageConverter
{
    PublishMessage ConvertForPublish(Message message, PublishMetadata publishMetadata, PublishDestinationType destinationType);
}

public interface IReceivedMessageConverter
{
    ReceivedMessage ConvertForReceive(Amazon.SQS.Model.Message message);
}
