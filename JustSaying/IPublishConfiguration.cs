using System.Collections.Generic;

namespace JustSaying
{
    public interface IPublishConfiguration
    {
        int PublishFailureReAttempts { get; set; }
        int PublishFailureBackoffMilliseconds { get; set; }
        IReadOnlyCollection<string> AdditionalSubscriberAccounts { get; set; }
    }
}
