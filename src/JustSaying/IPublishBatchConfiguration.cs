using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;

namespace JustSaying;

public interface IPublishBatchConfiguration
{
    int PublishFailureReAttempts { get; set; }
    TimeSpan PublishFailureBackoff { get; set; }
    Action<MessageBatchResponse, IReadOnlyCollection<Message>> MessageBatchResponseLogger { get; set; }
}
