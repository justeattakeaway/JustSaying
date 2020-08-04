using System;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    /// <summary>
    /// Contains validation for <see cref="ISubscriptionGroupSettings"/>.
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
    }
}
