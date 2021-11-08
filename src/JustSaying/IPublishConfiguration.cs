using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;

namespace JustSaying
{
    public interface IPublishConfiguration
    {
        int PublishFailureReAttempts { get; set; }
        TimeSpan PublishFailureBackoff { get; set; }
        Action<MessageResponse, Message> MessageResponseLogger { get; set; }
        IReadOnlyCollection<string> AdditionalSubscriberAccounts { get; set; }
    }
}
