namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <summary>
/// Handles creation of instances of <see cref="ISubscriptionGroup"/>.
/// </summary>
public interface ISubscriptionGroupFactory
{
    /// <summary>
    /// Creates a <see cref="ISubscriptionGroup"/> for the given configuration.
    /// </summary>
    /// <param name="defaults">The default values to use while building each <see cref="SubscriptionGroup"/>.</param>
    /// <param name="subscriptionGroupSettings">Definitions for each subscription group that
    /// should be part of the returned collection.</param>
    /// <returns>An <see cref="ISubscriptionGroup"/> to run.</returns>
    ISubscriptionGroup Create(
        SubscriptionGroupSettingsBuilder defaults,
        IDictionary<string, SubscriptionGroupConfigBuilder> subscriptionGroupSettings);
}