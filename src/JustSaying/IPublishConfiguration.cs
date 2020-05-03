using System;
using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying
{
    public interface IPublishConfiguration
    {
        int PublishFailureReAttempts { get; set; }
        TimeSpan PublishFailureBackoff { get; set; }
        Action<MessageResponse, object> MessageResponseLogger { get; set; }
        IReadOnlyCollection<string> AdditionalSubscriberAccounts { get; set; }
    }
}
