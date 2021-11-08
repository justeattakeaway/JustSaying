namespace JustSaying.Fluent;
//// TODO I don't like this name, but we can give it a better name later

/// <summary>
/// Defines a method for configuring an instance of <see cref="MessagingBusBuilder"/>.
/// </summary>
public interface IMessageBusConfigurationContributor
{
    /// <summary>
    /// Configures an <see cref="MessagingBusBuilder"/>.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    void Configure(MessagingBusBuilder builder);
}