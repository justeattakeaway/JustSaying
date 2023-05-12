using JustSaying.Messaging.Channels.Receive;

namespace JustSaying.Messaging.Channels.SubscriptionGroups;

public interface ISubscriptionGroupSettingPauseReceivingBusyWaitInterval
{
    /// <summary>
    /// Delay interval to use during busy wait when <see cref="IMessageReceivePauseSignal"/> is set to pause receiving.
    /// A larger value may reduce CPU usage while paused, but may delay when messages start being received.
    /// </summary>
    public TimeSpan PauseReceivingBusyWaitInterval { get; }
}
