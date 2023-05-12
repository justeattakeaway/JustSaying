using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <summary>
/// Contains validation for <see cref="SubscriptionGroupSettings"/> and related interfaces.
/// </summary>
public static class SubscriptionGroupSettingsValidation
{
    /// <summary>
    /// Runs validation on the given instance of <see cref="ISubscriptionGroupSettings"/>.
    /// </summary>
    /// <param name="settings">The settings to validate</param>
    public static void Validate(this ISubscriptionGroupSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (settings.Prefetch < 0)
        {
            throw new InvalidOperationException($"{nameof(settings.Prefetch)} cannot be negative.");
        }

        if (settings.Prefetch > MessageDefaults.MaxAmazonMessageCap)
        {
            throw new InvalidOperationException(
                $"{nameof(settings.Prefetch)} cannot be greater than {nameof(MessageDefaults.MaxAmazonMessageCap)}.");
        }

        if (settings.ReceiveBufferReadTimeout < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(settings.ReceiveBufferReadTimeout)} cannot be negative.");
        }

        if (settings.ReceiveMessagesWaitTime < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(settings.ReceiveMessagesWaitTime)} cannot be negative.");
        }

        if (settings.ReceiveMessagesWaitTime > TimeSpan.FromSeconds(20))
        {
            throw new InvalidOperationException($"{nameof(settings.ReceiveMessagesWaitTime)} cannot be longer than 20 seconds.");
        }

        if (settings.ConcurrencyLimit < 0)
        {
            throw new InvalidOperationException($"{nameof(settings.ConcurrencyLimit)} cannot be negative.");
        }

        if (settings.MultiplexerCapacity < 0)
        {
            throw new InvalidOperationException($"{nameof(settings.MultiplexerCapacity)} cannot be negative.");
        }

        if (settings.BufferSize < 0)
        {
            throw new InvalidOperationException($"{nameof(settings.BufferSize)} cannot be negative.");
        }
    }

    /// <summary>
    /// Runs validation on the given instance of <see cref="SubscriptionGroupSettings"/>.
    /// </summary>
    /// <param name="settings">The settings to validate</param>
    public static void Validate(this SubscriptionGroupSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (settings is ISubscriptionGroupSettings)
        {
            ((ISubscriptionGroupSettings) settings).Validate();
        }

        if (settings is ISubscriptionGroupSettingPauseReceivingBusyWaitInterval)
        {
            ((ISubscriptionGroupSettingPauseReceivingBusyWaitInterval) settings).Validate();
        }
    }

    /// <summary>
    /// Runs validation on the given instance of <see cref="ISubscriptionGroupSettingPauseReceivingBusyWaitInterval"/>.
    /// </summary>
    /// <param name="setting">The setting to validate</param>
    public static void Validate(this ISubscriptionGroupSettingPauseReceivingBusyWaitInterval setting)
    {
        if (setting is null)
        {
            throw new ArgumentNullException(nameof(setting));
        }

        if (setting.PauseReceivingBusyWaitInterval < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(setting.PauseReceivingBusyWaitInterval)} cannot be negative.");
        }
    }
}
